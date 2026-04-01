using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string PaymentReference { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public int BookingId { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UpdateCount { get; set; } = 0;
    }
}
