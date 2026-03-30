namespace Admitto.Core.Models.Responses.TicketTypes
{
    public class TicketTypeResponse
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
