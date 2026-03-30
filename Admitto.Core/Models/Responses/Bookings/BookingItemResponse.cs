namespace Admitto.Core.Models.Responses.Bookings
{
    public class BookingItemResponse
    {
        public int Id { get; set; }
        public int TicketTypeId { get; set; }
        public string TicketTypeName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
