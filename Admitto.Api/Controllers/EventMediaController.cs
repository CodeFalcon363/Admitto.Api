using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admitto.Api.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/media")]
    public class EventMediaController : ControllerBase
    {
        private readonly IEventMediaService _eventMediaService;

        public EventMediaController(IEventMediaService eventMediaService)
        {
            _eventMediaService = eventMediaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByEventId(int eventId)
        {
            var result = await _eventMediaService.GetByEventIdAsync(eventId);
            return Ok(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(int eventId, IFormFile file, [FromForm] MediaType type)
        {
            using var stream = file.OpenReadStream();
            var result = await _eventMediaService.UploadAsync(eventId, stream, file.FileName, type);
            return result.Success ? StatusCode(201, result) : BadRequest(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpDelete("{mediaId:int}")]
        public async Task<IActionResult> Delete(int eventId, int mediaId)
        {
            var result = await _eventMediaService.DeleteAsync(mediaId);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
