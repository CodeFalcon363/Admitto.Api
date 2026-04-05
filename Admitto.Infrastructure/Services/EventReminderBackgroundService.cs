using Admitto.Core.Data;
using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Admitto.Infrastructure.Services
{
    public class EventReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<EventReminderBackgroundService> _logger;

        private const string LockKey = "admitto:reminder_lock";
        private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);

        // Upper bound on any reminder window configured in the system.
        // Events starting further out than this will never qualify this tick.
        // Keeps the SQL result set small regardless of total event count.
        private const int MaxReminderWindowHours = 72;

        public EventReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConnectionMultiplexer redis,
            ILogger<EventReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunReminderCheckAsync();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task RunReminderCheckAsync()
        {
            var lockValue = Guid.NewGuid().ToString();
            var db = _redis.GetDatabase();
            var lockAcquired = false;

            try
            {
                lockAcquired = await db.StringSetAsync(LockKey, lockValue, LockExpiry, When.NotExists);
                if (!lockAcquired)
                {
                    _logger.LogInformation("Reminder check skipped — another instance holds the lock");
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AdmittoDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var now = DateTime.UtcNow;
                // Pre-filter in SQL: only events starting within the maximum possible reminder window.
                // The precise per-event window check (which varies by override/organizer/global rule)
                // is still applied in C# below, but the DB only returns a small bounded result set.
                var lookAheadCutoff = now.AddHours(MaxReminderWindowHours);

                var dueEvents = await (
                    from e in context.Events
                    join nr in context.NotificationRules
                        on (int)NotificationTrigger.EventReminder equals (int)nr.TriggerType
                    join ero in context.EventReminderOverrides
                        on e.Id equals ero.EventId into eroGroup
                    from ero in eroGroup.DefaultIfEmpty()
                    join ors in context.OrganizerReminderSettings
                        on e.OrganizerId equals ors.OrganizerId into orsGroup
                    from ors in orsGroup.DefaultIfEmpty()
                    where e.Status == EventStatus.Published
                        && e.ReminderSentAt == null
                        && nr.IsEnabled
                        && e.StartDate > now
                        && e.StartDate <= lookAheadCutoff  // SQL-side pre-filter
                    select new
                    {
                        Event = e,
                        ReminderHours = ero != null ? ero.ReminderHoursBefore
                                      : ors != null ? ors.ReminderHoursBefore
                                      : nr.ReminderHoursBefore ?? 24
                    }
                ).ToListAsync();

                foreach (var item in dueEvents)
                {
                    var windowStart = item.Event.StartDate.AddHours(-item.ReminderHours);
                    if (now < windowStart) continue;

                    try
                    {
                        await notificationService.SendEventReminderAsync(item.Event.Slug);

                        // Persist immediately after each successful send.
                        // This way a crash or error on the next event does not cause
                        // already-sent reminders to be re-sent on the next tick.
                        item.Event.ReminderSentAt = now;
                        await context.SaveChangesAsync();

                        _logger.LogInformation("Reminder sent and persisted for event {EventId}", item.Event.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reminder for event {EventId} — will retry next tick", item.Event.Id);
                        // Detach the entity so the failed ReminderSentAt=null state does not
                        // bleed into subsequent SaveChangesAsync calls for other events.
                        context.Entry(item.Event).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning("Redis unavailable — skipping reminder check, lock will expire naturally. Reason: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventReminderBackgroundService encountered an error");
            }
            finally
            {
                if (lockAcquired)
                {
                    try
                    {
                        const string releaseScript =
                            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
                        await db.ScriptEvaluateAsync(
                            releaseScript,
                            new RedisKey[] { LockKey },
                            new RedisValue[] { lockValue });
                    }
                    catch (RedisConnectionException) { /* Redis went down between acquire and release — lock expires naturally */ }
                }
            }
        }
    }
}
