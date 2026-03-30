namespace Admitto.Core.Models.Requests
{
    public class CreateBookingRequest
    {
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
