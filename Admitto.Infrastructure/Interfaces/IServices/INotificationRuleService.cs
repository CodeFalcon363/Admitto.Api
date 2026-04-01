using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Notifications;
using Admitto.Core.Models.Responses.Notifications;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface INotificationRuleService
    {
        Task<ApiResponse<IEnumerable<NotificationRuleResponse>>> GetAllAsync();
        Task<ApiResponse<NotificationRuleResponse>> UpdateAsync(int id, UpdateNotificationRuleRequest request);
    }
}
