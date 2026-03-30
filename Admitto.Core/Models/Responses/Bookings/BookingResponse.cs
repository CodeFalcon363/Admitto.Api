using Admitto.Core.Models;

namespace Admitto.Core.Models.Responses.Bookings
{
    public class BookingResponse
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public BookingStatus Status { get; set; }
        public string IdempotencyKey { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<BookingItemResponse> Items { get; set; } = new();
    }
}
