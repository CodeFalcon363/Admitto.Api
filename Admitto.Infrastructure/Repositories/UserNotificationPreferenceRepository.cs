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
            var triggerType = (int)preference.TriggerType;
            await _context.Database.ExecuteSqlAsync(
                $"""
                MERGE INTO UserNotificationPreferences WITH (HOLDLOCK) AS target
                USING (VALUES ({preference.UserId}, {triggerType})) AS source (UserId, TriggerType)
                ON target.UserId = source.UserId AND target.TriggerType = source.TriggerType
                WHEN MATCHED THEN
                    UPDATE SET IsEnabled = {preference.IsEnabled}, UpdatedAt = {preference.UpdatedAt}
                WHEN NOT MATCHED THEN
                    INSERT (UserId, TriggerType, IsEnabled, UpdatedAt)
                    VALUES ({preference.UserId}, {triggerType}, {preference.IsEnabled}, {preference.UpdatedAt});
                """);
        }
    }
}
