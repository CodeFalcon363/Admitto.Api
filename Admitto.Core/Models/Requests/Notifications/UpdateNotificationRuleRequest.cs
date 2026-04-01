namespace Admitto.Core.Models.Requests.Notifications
{
    public class UpdateNotificationRuleRequest
    {
        public bool IsEnabled { get; set; }
        public int? ReminderHoursBefore { get; set; }
    }
}
