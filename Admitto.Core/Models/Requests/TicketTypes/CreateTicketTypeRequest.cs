using System.ComponentModel.DataAnnotations;

namespace Admitto.Core.Models.Requests.TicketTypes
{
    public class CreateTicketTypeRequest
    {
        [Required]
        public int EventId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
        public DateTime? SaleEndDate { get; set; }
    }
}
