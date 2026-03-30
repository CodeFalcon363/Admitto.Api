namespace Admitto.Core.Models.Responses.Events
{
    public class ExternalEventResponse
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string ExternalBookingUrl { get; set; } = null!;
        public string Venue { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
