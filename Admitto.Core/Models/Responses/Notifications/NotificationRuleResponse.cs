namespace Admitto.Core.Models.Responses.Notifications
{
    public class NotificationRuleResponse
    {
        public int Id { get; set; }
        public NotificationTrigger TriggerType { get; set; }
        public bool IsEnabled { get; set; }
        public int? ReminderHoursBefore { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
