using Admitto.Core.Entities;
using Admitto.Core.Models;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface INotificationRuleRepository
    {
        Task<IEnumerable<NotificationRule>> GetAllAsync();
        Task<NotificationRule?> GetByIdAsync(int id);
        Task<NotificationRule?> GetByTriggerAsync(NotificationTrigger trigger);
        Task UpdateAsync(NotificationRule rule);
    }
}
