using System.ComponentModel.DataAnnotations;

namespace Admitto.Core.Models.Requests.Payments
{
    public class InitializePaymentRequest
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public string Currency { get; set; } = "NGN";
    }
}
