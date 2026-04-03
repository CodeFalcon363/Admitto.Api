using Admitto.Core.Models;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface INotificationResolver
    {
        Task<bool> ShouldSendAsync(Guid userId, NotificationTrigger trigger);
    }
}
