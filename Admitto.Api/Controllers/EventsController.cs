using Admitto.Core.Constants;
using Admitto.Core.Models.Requests.Events;
using Admitto.Infrastructure.Interfaces.IServices;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Admitto.Api.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        // 30-second TTL absorbs read spikes without serving significantly stale data.
        // Varies cache by pageNumber and pageSize so each page has its own entry.
        [OutputCache(Duration = 30, VaryByQueryKeys = ["pageNumber", "pageSize"])]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize   = Math.Clamp(pageSize, 1, 100);
            var result = await _eventService.GetAllAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _eventService.GetByIdAsync(id);
            return result.Success 
                ? Ok(result) 
                : NotFound(result);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _eventService.GetBySlugAsync(slug);
            return result.Success 
                ? Ok(result)
                : NotFound(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _eventService.CreateAsync(request, organizerId);
            return StatusCode(201, result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPut("{slug}")]
        public async Task<IActionResult> Update(string slug, [FromBody] UpdateEventRequest request)
        {
            var callerUserId = User.IsInRole(Roles.Admin)
                ? (Guid?)null
                : Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

            var result = await _eventService.UpdateAsync(slug, request, callerUserId);
            return result.Success 
                ? Ok(result) 
                : result.Message == ApiMessages.UnauthorizedAccess
                    ? Forbid()
                    : NotFound(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _eventService.DeleteAsync(id);
            return result.Success
                ? Ok(result) 
                : NotFound(result);
        }
    }
}
