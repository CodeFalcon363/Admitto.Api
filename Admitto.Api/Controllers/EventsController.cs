using Admitto.Core.Constants;
using Admitto.Core.Models.Requests.Events;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admitto.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _eventService.GetAllAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _eventService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _eventService.GetBySlugAsync(slug);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
        {
            var result = await _eventService.CreateAsync(request);
            return StatusCode(201, result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPut("{slug}")]
        public async Task<IActionResult> Update(string slug, [FromBody] UpdateEventRequest request)
        {
            var result = await _eventService.UpdateAsync(slug, request);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _eventService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
