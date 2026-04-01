using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class UserNotificationPreference
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public NotificationTrigger TriggerType { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
