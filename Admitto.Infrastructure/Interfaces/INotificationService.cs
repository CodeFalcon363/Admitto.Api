namespace Admitto.Infrastructure.Interfaces
{
    public interface INotificationService
    {
        Task SendBookingConfirmationAsync(int bookingId);
        Task SendEventReminderAsync(int eventSlug);
        Task SendCancellationAsync(int bookingId);
    }
}
