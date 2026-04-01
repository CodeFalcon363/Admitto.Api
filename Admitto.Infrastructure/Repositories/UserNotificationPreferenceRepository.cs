using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class UserNotificationPreferenceRepository : IUserNotificationPreferenceRepository
    {
        private readonly AdmittoDbContext _context;

        public UserNotificationPreferenceRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserNotificationPreference>> GetByUserAsync(Guid userId)
            => await _context.UserNotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

        public async Task<UserNotificationPreference?> GetByUserAndTriggerAsync(Guid userId, NotificationTrigger trigger)
            => await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TriggerType == trigger);

        public async Task UpsertAsync(UserNotificationPreference preference)
        {
            var existing = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == preference.UserId && p.TriggerType == preference.TriggerType);

            if (existing == null)
                _context.UserNotificationPreferences.Add(preference);
            else
            {
                existing.IsEnabled = preference.IsEnabled;
                existing.UpdatedAt = preference.UpdatedAt;
            }

            await _context.SaveChangesAsync();
        }
    }
}
