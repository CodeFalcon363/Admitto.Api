namespace Admitto.Core.Entities
{
    public class TicketType
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int EventId { get; set; }
        public int Capacity { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UpdateCount { get; set; } = 0;
    }
}
