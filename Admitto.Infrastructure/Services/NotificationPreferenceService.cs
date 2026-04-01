using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Notifications;
using Admitto.Core.Models.Responses.Notifications;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;

namespace Admitto.Infrastructure.Services
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly IUserNotificationPreferenceRepository _preferenceRepository;
        private readonly IEventReminderOverrideRepository _eventOverrideRepository;
        private readonly IOrganizerReminderSettingRepository _organizerSettingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public NotificationPreferenceService(
            IUserNotificationPreferenceRepository preferenceRepository,
            IEventReminderOverrideRepository eventOverrideRepository,
            IOrganizerReminderSettingRepository organizerSettingRepository,
            IEventRepository eventRepository,
            IMapper mapper)
        {
            _preferenceRepository = preferenceRepository;
            _eventOverrideRepository = eventOverrideRepository;
            _organizerSettingRepository = organizerSettingRepository;
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<UserPreferenceResponse>>> GetMyPreferencesAsync(Guid userId)
        {
            var preferences = await _preferenceRepository.GetByUserAsync(userId);
            return new ApiResponse<IEnumerable<UserPreferenceResponse>>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<UserPreferenceResponse>>(preferences)
            };
        }

        public async Task<ApiResponse<UserPreferenceResponse>> SetPreferenceAsync(Guid userId, SetUserPreferenceRequest request)
        {
            var preference = new UserNotificationPreference
            {
                UserId = userId,
                TriggerType = request.TriggerType,
                IsEnabled = request.IsEnabled,
                UpdatedAt = DateTime.UtcNow
            };

            await _preferenceRepository.UpsertAsync(preference);

            return new ApiResponse<UserPreferenceResponse>
            {
                Success = true,
                Message = ApiMessages.PreferenceUpdated,
                Data = _mapper.Map<UserPreferenceResponse>(preference)
            };
        }

        public async Task<ApiResponse<int>> GetReminderSettingAsync(Guid organizerId)
        {
            var setting = await _organizerSettingRepository.GetByOrganizerAsync(organizerId);
            return new ApiResponse<int>
            {
                Success = true,
                Data = setting?.ReminderHoursBefore ?? 24
            };
        }

        public async Task<ApiResponse<object>> SetAccountReminderHoursAsync(Guid organizerId, SetReminderHoursRequest request)
        {
            var setting = new OrganizerReminderSetting
            {
                OrganizerId = organizerId,
                ReminderHoursBefore = request.ReminderHoursBefore,
                UpdatedAt = DateTime.UtcNow
            };

            await _organizerSettingRepository.UpsertAsync(setting);

            return new ApiResponse<object> { Success = true, Message = ApiMessages.ReminderHoursUpdated };
        }

        public async Task<ApiResponse<object>> SetEventReminderHoursAsync(Guid organizerId, int eventId, SetReminderHoursRequest request)
        {
            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev == null)
                return new ApiResponse<object> { Success = false, Message = ApiMessages.EventNotFound };

            if (ev.OrganizerId != organizerId)
                return new ApiResponse<object> { Success = false, Message = ApiMessages.Unauthorized };

            var eventOverride = new EventReminderOverride
            {
                EventId = eventId,
                ReminderHoursBefore = request.ReminderHoursBefore,
                UpdatedAt = DateTime.UtcNow
            };

            await _eventOverrideRepository.UpsertAsync(eventOverride);

            return new ApiResponse<object> { Success = true, Message = ApiMessages.ReminderHoursUpdated };
        }
    }
}
