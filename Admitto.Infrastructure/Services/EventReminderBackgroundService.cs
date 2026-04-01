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

        public EventReminderBackgroundService(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis, ILogger<EventReminderBackgroundService> logger)
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

                    await notificationService.SendEventReminderAsync(item.Event.Slug);

                    item.Event.ReminderSentAt = now;
                }

                if (dueEvents.Any(d => d.Event.ReminderSentAt != null))
                    await context.SaveChangesAsync();
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable — skipping reminder check, lock will expire naturally");
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
                        const string releaseScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
                        await db.ScriptEvaluateAsync(releaseScript, new RedisKey[] { LockKey }, new RedisValue[] { lockValue });
                    }
                    catch (RedisConnectionException) { /* Redis went down between acquire and release — lock expires naturally */ }
                }
            }
        }
    }
}
