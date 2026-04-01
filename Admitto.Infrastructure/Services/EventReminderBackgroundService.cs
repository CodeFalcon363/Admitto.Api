using Admitto.Core.Data;
using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class EventReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventReminderBackgroundService> _logger;

        public EventReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<EventReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
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
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventReminderBackgroundService encountered an error");
            }
        }
    }
}
