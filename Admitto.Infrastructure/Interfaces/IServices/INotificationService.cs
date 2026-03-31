namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface INotificationService
    {
        Task SendBookingConfirmationAsync(int bookingId);
        Task SendEventReminderAsync(int eventSlug);
        Task SendCancellationAsync(int bookingId);
    }
}
