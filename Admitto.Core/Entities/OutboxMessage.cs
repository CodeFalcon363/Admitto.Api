namespace Admitto.Core.Entities
{
    /// <summary>
    /// Durable queue for notification delivery.
    /// Written in the same request scope as the business operation — if the process
    /// crashes before a Task.Run notification fires, the outbox row survives and the
    /// background processor retries on the next tick.
    /// </summary>
    public class OutboxMessage
    {
        public int Id { get; set; }

        /// <summary>Notification type — matches a method on INotificationService.</summary>
        public string EventType { get; set; } = null!;

        /// <summary>JSON payload containing the IDs / arguments for the notification method.</summary>
        public string Payload { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Error { get; set; }
        public int RetryCount { get; set; }
    }
}
