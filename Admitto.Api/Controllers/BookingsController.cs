using Admitto.Core.Models.Requests.Bookings;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Admitto.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _bookingService.GetAllByUserIdAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _bookingService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
        {
            var result = await _bookingService.CreateAsync(request);
            return result.Success ? StatusCode(201, result) : BadRequest(result);
        }

        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _bookingService.CancelAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
