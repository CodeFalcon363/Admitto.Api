namespace Admitto.Core.Entities
{
    public class OrganizerReminderSetting
    {
        public int Id { get; set; }
        public Guid OrganizerId { get; set; }
        public int ReminderHoursBefore { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
