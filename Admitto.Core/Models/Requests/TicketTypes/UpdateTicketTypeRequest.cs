using System.ComponentModel.DataAnnotations;

namespace Admitto.Core.Models.Requests.TicketTypes
{
    public class UpdateTicketTypeRequest
    {
        public string? Name { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }
        [Range(1, int.MaxValue)]
        public int? Capacity { get; set; }
        public DateTime? SaleEndDate { get; set; }
    }
}
