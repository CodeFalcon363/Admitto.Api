using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Payments;
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
    [Route("api/v{version:apiVersion}/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // Admin is included so they can initialize payment for a booking they created.
        [Authorize(Roles = $"{Roles.Attendee},{Roles.Admin}")]
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializePaymentRequest request)
        {
            var callerUserId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await _paymentService.InitializeAsync(request, callerUserId);
            return result.Success 
                ? Ok(result) 
                : result.Message == ApiMessages.UnauthorizedAccess
                    ? Forbid()
                    : BadRequest(result);
        }

        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> Verify(string reference)
        {
            var callerUserId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var isAdmin = User.IsInRole(Roles.Admin);
            var result = await _paymentService.VerifyAsync(reference, callerUserId, isAdmin);
            return result.Success 
                ? Ok(result) 
                : result.Message == ApiMessages.UnauthorizedAccess
                    ? Forbid()
                    : NotFound(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var callerUserId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var isAdmin = User.IsInRole(Roles.Admin);
            var result = await _paymentService.GetByIdAsync(id, callerUserId, isAdmin);
            return result.Success 
                ? Ok(result) 
                : result.Message == ApiMessages.UnauthorizedAccess
                    ? Forbid()
                    : NotFound(result);
        }
    }
}
