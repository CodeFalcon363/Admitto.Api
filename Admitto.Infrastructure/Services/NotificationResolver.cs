using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;

namespace Admitto.Infrastructure.Services
{
    public class NotificationResolver : INotificationResolver
    {
        private readonly INotificationRuleRepository _ruleRepository;
        private readonly IUserNotificationPreferenceRepository _preferenceRepository;
        private readonly IEventReminderOverrideRepository _eventOverrideRepository;
        private readonly IOrganizerReminderSettingRepository _organizerSettingRepository;

        public NotificationResolver(
            INotificationRuleRepository ruleRepository,
            IUserNotificationPreferenceRepository preferenceRepository,
            IEventReminderOverrideRepository eventOverrideRepository,
            IOrganizerReminderSettingRepository organizerSettingRepository)
        {
            _ruleRepository = ruleRepository;
            _preferenceRepository = preferenceRepository;
            _eventOverrideRepository = eventOverrideRepository;
            _organizerSettingRepository = organizerSettingRepository;
        }

        public async Task<bool> ShouldSendAsync(Guid userId, NotificationTrigger trigger)
        {
            var rule = await _ruleRepository.GetByTriggerAsync(trigger);
            if (rule == null || !rule.IsEnabled)
                return false;

            var preference = await _preferenceRepository.GetByUserAndTriggerAsync(userId, trigger);
            if (preference != null)
                return preference.IsEnabled;

            return true;
        }

        public async Task<int> GetReminderHoursAsync(int eventId, Guid organizerId)
        {
            var eventOverride = await _eventOverrideRepository.GetByEventAsync(eventId);
            if (eventOverride != null)
                return eventOverride.ReminderHoursBefore;

            var organizerSetting = await _organizerSettingRepository.GetByOrganizerAsync(organizerId);
            if (organizerSetting != null)
                return organizerSetting.ReminderHoursBefore;

            var rule = await _ruleRepository.GetByTriggerAsync(NotificationTrigger.EventReminder);
            if (rule?.ReminderHoursBefore != null)
                return rule.ReminderHoursBefore.Value;

            return 24;
        }
    }
}
