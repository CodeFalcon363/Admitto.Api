using Admitto.Core.Models;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Admitto.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly INotificationResolver _resolver;
        private readonly NotificationSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IBookingRepository bookingRepository,
            IUserRepository userRepository,
            IEventRepository eventRepository,
            INotificationResolver resolver,
            IOptions<NotificationSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<NotificationService> logger)
        {
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _resolver = resolver;
            _settings = settings.Value;
            _httpClient = httpClientFactory.CreateClient("notification");
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(int bookingId)
        {
            var (attendee, booking) = await GetAttendeeForBooking(bookingId);
            if (attendee == null || booking == null) return;

            if (!await _resolver.ShouldSendAsync(attendee.Id, NotificationTrigger.BookingConfirmation)) return;

            await SendEmailAsync(
                to: attendee.Email,
                subject: "Booking Confirmed",
                body: $"Hi {attendee.FirstName}, your booking #{booking.Id} has been confirmed. Thank you!"
            );
        }

        public async Task SendCancellationAsync(int bookingId)
        {
            var (attendee, booking) = await GetAttendeeForBooking(bookingId);
            if (attendee == null || booking == null) return;

            if (!await _resolver.ShouldSendAsync(attendee.Id, NotificationTrigger.BookingCancellation)) return;

            await SendEmailAsync(
                to: attendee.Email,
                subject: "Booking Cancelled",
                body: $"Hi {attendee.FirstName}, your booking #{booking.Id} has been cancelled."
            );
        }

        public async Task SendEventReminderAsync(string eventSlug)
        {
            var ev = await _eventRepository.GetBySlugAsync(eventSlug);
            if (ev == null)
            {
                _logger.LogWarning("SendEventReminder: event {Slug} not found", eventSlug);
                return;
            }

            var bookings = (await _bookingRepository.GetAllByEventSlugAsync(eventSlug)).ToList();
            if (bookings.Count == 0) return;

            // Batch-load all attendees in a single query instead of one per booking.
            var attendeeMap = await _userRepository.GetByIdsAsync(bookings.Select(b => b.UserId));

            foreach (var booking in bookings)
            {
                if (!attendeeMap.TryGetValue(booking.UserId, out var attendee)) continue;

                if (!await _resolver.ShouldSendAsync(attendee.Id, NotificationTrigger.EventReminder)) continue;

                await SendEmailAsync(
                    to: attendee.Email,
                    subject: $"Reminder: {ev.Title} is coming up",
                    body: $"Hi {attendee.FirstName}, just a reminder that \"{ev.Title}\" starts on {ev.StartDate:MMMM dd, yyyy}. We look forward to seeing you!"
                );
            }
        }

        public async Task SendEventCreatedAsync(int eventId)
        {
            var (organizer, ev) = await GetOrganizerForEvent(eventId);
            if (organizer == null || ev == null) return;

            if (!await _resolver.ShouldSendAsync(organizer.Id, NotificationTrigger.EventCreated)) return;

            await SendEmailAsync(
                to: organizer.Email,
                subject: "Event Created Successfully",
                body: $"Hi {organizer.FirstName}, your event \"{ev.Title}\" has been created and is now live."
            );
        }

        public async Task SendEventUpdatedAsync(int eventId)
        {
            var (organizer, ev) = await GetOrganizerForEvent(eventId);
            if (organizer == null || ev == null) return;

            if (!await _resolver.ShouldSendAsync(organizer.Id, NotificationTrigger.EventUpdated)) return;

            await SendEmailAsync(
                to: organizer.Email,
                subject: "Event Updated",
                body: $"Hi {organizer.FirstName}, your event \"{ev.Title}\" has been updated successfully."
            );
        }

        public async Task SendEventDeletedAsync(Guid organizerId, string eventTitle)
        {
            var organizer = await _userRepository.GetByIdAsync(organizerId);
            if (organizer == null) return;

            if (!await _resolver.ShouldSendAsync(organizer.Id, NotificationTrigger.EventDeleted)) return;

            await SendEmailAsync(
                to: organizer.Email,
                subject: "Event Deleted",
                body: $"Hi {organizer.FirstName}, your event \"{eventTitle}\" has been deleted."
            );
        }

        public async Task SendRoleChangedAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return;

            if (!await _resolver.ShouldSendAsync(user.Id, NotificationTrigger.RoleChanged)) return;

            await SendEmailAsync(
                to: user.Email,
                subject: "You're now an Organizer!",
                body: $"Hi {user.FirstName}, your account has been upgraded to Organizer. You can now create and manage events."
            );
        }

        public async Task SendProfileUpdatedAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return;

            if (!await _resolver.ShouldSendAsync(user.Id, NotificationTrigger.ProfileUpdated)) return;

            await SendEmailAsync(
                to: user.Email,
                subject: "Profile Updated",
                body: $"Hi {user.FirstName}, your profile has been updated successfully. If you didn't make this change, please contact support."
            );
        }

        public async Task SendPasswordResetAsync(string email, string firstName, string resetToken)
        {
            // PasswordReset is always on — never checked against resolver
            await SendEmailAsync(
                to: email,
                subject: "Password Reset Request",
                body: $"Hi {firstName}, use the following token to reset your password: {resetToken}. This token expires in 1 hour."
            );
        }

        private async Task<(Core.Entities.User? attendee, Core.Entities.Booking? booking)> GetAttendeeForBooking(int bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                _logger.LogWarning("Notification: booking {BookingId} not found", bookingId);
                return (null, null);
            }

            var attendee = await _userRepository.GetByIdAsync(booking.UserId);
            if (attendee == null)
                _logger.LogWarning("Notification: user {UserId} not found", booking.UserId);

            return (attendee, booking);
        }

        private async Task<(Core.Entities.User? organizer, Core.Entities.Event? ev)> GetOrganizerForEvent(int eventId)
        {
            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev == null)
            {
                _logger.LogWarning("Notification: event {EventId} not found", eventId);
                return (null, null);
            }

            var organizer = await _userRepository.GetByIdAsync(ev.OrganizerId);
            if (organizer == null)
                _logger.LogWarning("Notification: organizer {OrganizerId} not found", ev.OrganizerId);

            return (organizer, ev);
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var payload = new { from = _settings.SenderEmail, to, subject, body };
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/email")
                {
                    Content = JsonContent.Create(payload),
                    Headers = { { "Authorization", $"Bearer {_settings.ApiKey}" } }
                };

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Email send failed: {StatusCode} — {Content}", response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending email to {To}", to);
            }
        }
    }
}
