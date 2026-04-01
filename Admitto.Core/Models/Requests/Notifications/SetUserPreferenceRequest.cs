namespace Admitto.Core.Models.Requests.Notifications
{
    public class SetUserPreferenceRequest
    {
        public NotificationTrigger TriggerType { get; set; }
        public bool IsEnabled { get; set; }
    }
}
