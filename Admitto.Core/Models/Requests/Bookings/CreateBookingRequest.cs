using System.ComponentModel.DataAnnotations;

namespace Admitto.Core.Models.Requests.Bookings
{
    public class CreateBookingRequest
    {
        [Required]
        public List<BookingItemRequest> Items { get; set; } = new();
        [Required]
        public string IdempotencyKey { get; set; } = null!;
    }
}
