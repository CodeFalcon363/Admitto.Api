using Admitto.Core.Models;

namespace Admitto.Core.Models.Responses.Payments
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string PaymentReference { get; set; } = null!;
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
