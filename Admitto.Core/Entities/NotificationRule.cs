using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class NotificationRule
    {
        public int Id { get; set; }
        public NotificationTrigger TriggerType { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int? ReminderHoursBefore { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
