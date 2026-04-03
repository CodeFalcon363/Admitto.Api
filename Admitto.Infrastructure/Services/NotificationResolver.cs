using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;

namespace Admitto.Infrastructure.Services
{
    public class NotificationResolver : INotificationResolver
    {
        private readonly INotificationRuleRepository _ruleRepository;
        private readonly IUserNotificationPreferenceRepository _preferenceRepository;
        private readonly ICacheService _cache;

        // Notification rules are global config — they change only via admin action.
        // A 5-minute TTL means at most a 5-minute lag after an admin flips a rule.
        private static readonly TimeSpan RuleTtl = TimeSpan.FromMinutes(5);

        // Per-user preferences change on user action — 1 minute is a reasonable lag.
        private static readonly TimeSpan PreferenceTtl = TimeSpan.FromMinutes(1);

        public NotificationResolver(
            INotificationRuleRepository ruleRepository,
            IUserNotificationPreferenceRepository preferenceRepository,
            ICacheService cache)
        {
            _ruleRepository = ruleRepository;
            _preferenceRepository = preferenceRepository;
            _cache = cache;
        }

        public async Task<bool> ShouldSendAsync(Guid userId, NotificationTrigger trigger)
        {
            var rule = await GetRuleAsync(trigger);
            if (rule == null || !rule.IsEnabled)
                return false;

            var preference = await GetUserPreferenceAsync(userId, trigger);
            if (preference != null)
                return preference.IsEnabled;

            return true;
        }

        // ── Private cached fetchers ────────────────────────────────────────────────

        private async Task<Core.Entities.NotificationRule?> GetRuleAsync(NotificationTrigger trigger)
        {
            var key = $"notif:rule:{(int)trigger}";
            var cached = await _cache.GetAsync<Core.Entities.NotificationRule>(key);
            if (cached != null) return cached;

            var rule = await _ruleRepository.GetByTriggerAsync(trigger);
            if (rule != null)
                await _cache.SetAsync(key, rule, RuleTtl);

            return rule;
        }

        private async Task<Core.Entities.UserNotificationPreference?> GetUserPreferenceAsync(
            Guid userId, NotificationTrigger trigger)
        {
            var key = $"notif:pref:{userId}:{(int)trigger}";
            // Cache miss vs "user has no preference" must be distinguishable.
            // We use a sentinel wrapper so a null result (no preference set) is also cached,
            // preventing a DB round-trip for every notification to users who use defaults.
            var cached = await _cache.GetAsync<PreferenceWrapper>(key);
            if (cached != null) return cached.Preference;

            var preference = await _preferenceRepository.GetByUserAndTriggerAsync(userId, trigger);
            await _cache.SetAsync(key, new PreferenceWrapper(preference), PreferenceTtl);

            return preference;
        }

        /// <summary>
        /// Wraps a nullable preference so a missing preference (null) can be cached
        /// and distinguished from a cache miss (absent key), preventing per-notification
        /// DB round-trips for users who rely on the global default.
        /// </summary>
        private record PreferenceWrapper(Core.Entities.UserNotificationPreference? Preference);
    }
}
