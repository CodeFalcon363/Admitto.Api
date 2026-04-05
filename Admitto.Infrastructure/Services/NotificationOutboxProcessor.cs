using Admitto.Core.Constants;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Admitto.Infrastructure.Services
{
    /// <summary>
    /// Background processor that drains the OutboxMessages table every 30 seconds.
    /// Retries failed messages up to 3 times before marking them permanently failed.
    /// Using IServiceScopeFactory because the processor is Singleton but its
    /// dependencies (DbContext, repositories) are Scoped.
    /// </summary>
    public class NotificationOutboxProcessor : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationOutboxProcessor> _logger;

        public NotificationOutboxProcessor(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationOutboxProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken stoppingToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var outbox       = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();

            List<Core.Entities.OutboxMessage> pending;
            try
            {
                pending = await outbox.GetPendingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read outbox — will retry next tick");
                return;
            }

            foreach (var message in pending)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await DispatchAsync(notification, message.EventType, message.Payload);
                    await outbox.MarkProcessedAsync(message.Id);
                    _logger.LogInformation("Outbox message {Id} ({EventType}) processed", message.Id, message.EventType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox message {Id} ({EventType}) failed — incrementing retry count", message.Id, message.EventType);
                    await outbox.MarkFailedAsync(message.Id, ex.Message);
                }
            }
        }

        private static Task DispatchAsync(INotificationService svc, string eventType, string payload)
        {
            return eventType switch
            {
                OutboxEventTypes.BookingConfirmation => svc.SendBookingConfirmationAsync(
                    JsonConvert.DeserializeObject<IntPayload>(payload)!.Id),

                OutboxEventTypes.BookingCancellation => svc.SendCancellationAsync(
                    JsonConvert.DeserializeObject<IntPayload>(payload)!.Id),

                OutboxEventTypes.EventCreated => svc.SendEventCreatedAsync(
                    JsonConvert.DeserializeObject<IntPayload>(payload)!.Id),

                OutboxEventTypes.EventUpdated => svc.SendEventUpdatedAsync(
                    JsonConvert.DeserializeObject<IntPayload>(payload)!.Id),

                OutboxEventTypes.EventDeleted => svc.SendEventDeletedAsync(
                    JsonConvert.DeserializeObject<EventDeletedPayload>(payload)!.OrganizerId,
                    JsonConvert.DeserializeObject<EventDeletedPayload>(payload)!.EventTitle),

                OutboxEventTypes.RoleChanged => svc.SendRoleChangedAsync(
                    JsonConvert.DeserializeObject<GuidPayload>(payload)!.Id),

                OutboxEventTypes.ProfileUpdated => svc.SendProfileUpdatedAsync(
                    JsonConvert.DeserializeObject<GuidPayload>(payload)!.Id),

                _ => throw new InvalidOperationException($"Unknown outbox event type: {eventType}")
            };
        }

        private record IntPayload(int Id);
        private record GuidPayload(Guid Id);
        private record EventDeletedPayload(Guid OrganizerId, string EventTitle);
    }
}
