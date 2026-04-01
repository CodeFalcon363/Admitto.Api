using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public Guid OrganizerId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public EventStatus Status{ get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UpdateCount { get; set; } = 0;
        public DateTime? ReminderSentAt { get; set; }
    }
}
