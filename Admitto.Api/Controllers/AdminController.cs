using Admitto.Core.Constants;
using Admitto.Core.Models.Requests.Notifications;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admitto.Api.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly INotificationRuleService _notificationRuleService;

        public AdminController(INotificationRuleService notificationRuleService)
        {
            _notificationRuleService = notificationRuleService;
        }

        [HttpGet("notification-rules")]
        public async Task<IActionResult> GetNotificationRules()
        {
            var result = await _notificationRuleService.GetAllAsync();
            return Ok(result);
        }

        [HttpPut("notification-rules/{id:int}")]
        public async Task<IActionResult> UpdateNotificationRule(int id, [FromBody] UpdateNotificationRuleRequest request)
        {
            var result = await _notificationRuleService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
