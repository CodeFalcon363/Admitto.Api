using Admitto.Core.Constants;
using Admitto.Core.Models.Requests.Users;
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
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            if (currentUserId != id && !User.IsInRole(Roles.Admin))
                return Forbid();

            var result = await _userService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _userService.UpdateProfileAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:guid}/role")]
        public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
        {
            var result = await _userService.ChangeRoleAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
