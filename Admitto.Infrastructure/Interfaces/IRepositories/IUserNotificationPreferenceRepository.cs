using Admitto.Core.Entities;
using Admitto.Core.Models;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IUserNotificationPreferenceRepository
    {
        Task<IEnumerable<UserNotificationPreference>> GetByUserAsync(Guid userId);
        Task<UserNotificationPreference?> GetByUserAndTriggerAsync(Guid userId, NotificationTrigger trigger);
        Task UpsertAsync(UserNotificationPreference preference);
    }
}
