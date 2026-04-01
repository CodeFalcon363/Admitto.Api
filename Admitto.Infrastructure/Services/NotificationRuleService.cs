using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Notifications;
using Admitto.Core.Models.Responses.Notifications;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;

namespace Admitto.Infrastructure.Services
{
    public class NotificationRuleService : INotificationRuleService
    {
        private readonly INotificationRuleRepository _ruleRepository;
        private readonly IMapper _mapper;

        public NotificationRuleService(INotificationRuleRepository ruleRepository, IMapper mapper)
        {
            _ruleRepository = ruleRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<NotificationRuleResponse>>> GetAllAsync()
        {
            var rules = await _ruleRepository.GetAllAsync();
            return new ApiResponse<IEnumerable<NotificationRuleResponse>>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<NotificationRuleResponse>>(rules)
            };
        }

        public async Task<ApiResponse<NotificationRuleResponse>> UpdateAsync(int id, UpdateNotificationRuleRequest request)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
                return new ApiResponse<NotificationRuleResponse> { Success = false, Message = ApiMessages.NotificationRuleNotFound };

            rule.IsEnabled = request.IsEnabled;
            rule.ReminderHoursBefore = request.ReminderHoursBefore;
            rule.UpdatedAt = DateTime.UtcNow;

            await _ruleRepository.UpdateAsync(rule);
            return new ApiResponse<NotificationRuleResponse>
            {
                Success = true,
                Message = ApiMessages.NotificationRuleUpdated,
                Data = _mapper.Map<NotificationRuleResponse>(rule)
            };
        }
    }
}
