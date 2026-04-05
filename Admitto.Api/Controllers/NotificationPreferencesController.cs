using Admitto.Core.Constants;
using Admitto.Core.Models.Requests.Notifications;
using Admitto.Infrastructure.Interfaces.IServices;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Admitto.Api.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/notification-preferences")]
    public class NotificationPreferencesController : ControllerBase
    {
        private readonly INotificationPreferenceService _preferenceService;

        public NotificationPreferencesController(INotificationPreferenceService preferenceService)
        {
            _preferenceService = preferenceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPreferences()
        {
            var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _preferenceService.GetMyPreferencesAsync(userId);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SetPreference([FromBody] SetUserPreferenceRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _preferenceService.SetPreferenceAsync(userId, request);
            return result.Success 
                ? Ok(result) 
                : BadRequest(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpGet("reminder")]
        public async Task<IActionResult> GetReminderSetting()
        {
            var organizerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _preferenceService.GetReminderSettingAsync(organizerId);
            return Ok(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPut("reminder")]
        public async Task<IActionResult> SetAccountReminderHours([FromBody] SetReminderHoursRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _preferenceService.SetAccountReminderHoursAsync(organizerId, request);
            return result.Success 
                ? Ok(result)
                : BadRequest(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPut("events/{eventId:int}/reminder")]
        public async Task<IActionResult> SetEventReminderHours(int eventId, [FromBody] SetReminderHoursRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _preferenceService.SetEventReminderHoursAsync(organizerId, eventId, request);
            return result.Success 
                ? Ok(result) 
                : BadRequest(result);
        }
    }
}
