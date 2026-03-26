using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string IdempotencyKey { get; set; } = null!;

    }
}
