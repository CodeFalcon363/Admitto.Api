using System.ComponentModel.DataAnnotations;

namespace Admitto.Core.Models.Requests.Bookings
{
    public class BookingItemRequest
    {
        [Required]
        public int TicketTypeId { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
