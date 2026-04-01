namespace Admitto.Core.Entities
{
    public class EventReminderOverride
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int ReminderHoursBefore { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
