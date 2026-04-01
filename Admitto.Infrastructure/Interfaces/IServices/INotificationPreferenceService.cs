using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Notifications;
using Admitto.Core.Models.Responses.Notifications;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface INotificationPreferenceService
    {
        Task<ApiResponse<IEnumerable<UserPreferenceResponse>>> GetMyPreferencesAsync(Guid userId);
        Task<ApiResponse<UserPreferenceResponse>> SetPreferenceAsync(Guid userId, SetUserPreferenceRequest request);
        Task<ApiResponse<int>> GetReminderSettingAsync(Guid organizerId);
        Task<ApiResponse<object>> SetAccountReminderHoursAsync(Guid organizerId, SetReminderHoursRequest request);
        Task<ApiResponse<object>> SetEventReminderHoursAsync(Guid organizerId, int eventId, SetReminderHoursRequest request);
    }
}
