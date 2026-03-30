namespace Admitto.Core.Models.Responses
{
    public class BookingResponse
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
