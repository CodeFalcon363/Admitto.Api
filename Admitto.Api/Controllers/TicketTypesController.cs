using Admitto.Core.Constants;
using Admitto.Core.Models.Requests.TicketTypes;
using Admitto.Infrastructure.Interfaces.IServices;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admitto.Api.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/ticket-types")]
    public class TicketTypesController : ControllerBase
    {
        private readonly ITicketTypeService _ticketTypeService;

        public TicketTypesController(ITicketTypeService ticketTypeService)
        {
            _ticketTypeService = ticketTypeService;
        }

        [HttpGet("event/{slug}")]
        public async Task<IActionResult> GetAllByEventSlug(string slug, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize   = Math.Clamp(pageSize, 1, 100);
            var result = await _ticketTypeService.GetAllByEventSlugAsync(slug, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _ticketTypeService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketTypeRequest request)
        {
            var result = await _ticketTypeService.CreateAsync(request);
            return StatusCode(201, result);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Organizer}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketTypeRequest request)
        {
            var result = await _ticketTypeService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _ticketTypeService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
