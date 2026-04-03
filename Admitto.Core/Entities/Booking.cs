using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UpdateCount { get; set; } = 0;
        public string IdempotencyKey { get; set; } = null!;

        public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
    }
}
