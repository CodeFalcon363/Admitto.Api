namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface INotificationService
    {
        // Booking
        Task SendBookingConfirmationAsync(int bookingId);
        Task SendCancellationAsync(int bookingId);
        Task SendEventReminderAsync(string eventSlug);

        // Events
        Task SendEventCreatedAsync(int eventId);
        Task SendEventUpdatedAsync(int eventId);
        Task SendEventDeletedAsync(Guid organizerId, string eventTitle);

        // Users
        Task SendRoleChangedAsync(Guid userId);
        Task SendProfileUpdatedAsync(Guid userId);
        Task SendPasswordResetAsync(string email, string firstName, string resetToken);
    }
}
