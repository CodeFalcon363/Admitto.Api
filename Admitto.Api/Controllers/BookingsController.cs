using Admitto.Core.Constants;
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
            pageNumber = Math.Max(1, pageNumber);
            pageSize   = Math.Clamp(pageSize, 1, 100);

            if (User.IsInRole(Roles.Admin))
            {
                var all = await _bookingService.GetAllAsync(pageNumber, pageSize);
                return Ok(all);
            }

            var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _bookingService.GetAllByUserIdAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var callerUserId = User.IsInRole(Roles.Admin)
                ? (Guid?)null
                : Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

            var result = await _bookingService.GetByIdAsync(id, callerUserId);
            return result.Success ? Ok(result) : result.Message == ApiMessages.UnauthorizedAccess
                ? Forbid()
                : NotFound(result);
        }

        [Authorize(Roles = $"{Roles.Attendee},{Roles.Admin}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _bookingService.CreateAsync(request, userId);
            return result.Success ? StatusCode(201, result) : BadRequest(result);
        }

        [Authorize(Roles = $"{Roles.Attendee},{Roles.Admin}")]
        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var callerUserId = User.IsInRole(Roles.Admin)
                ? (Guid?)null
                : Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

            var result = await _bookingService.CancelAsync(id, callerUserId);
            return result.Success ? Ok(result) : result.Message == ApiMessages.UnauthorizedAccess
                ? Forbid()
                : NotFound(result);
        }
    }
}
