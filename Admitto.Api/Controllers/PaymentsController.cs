using Admitto.Core.Models.Requests.Payments;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admitto.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializePaymentRequest request)
        {
            var result = await _paymentService.InitializeAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> Verify(string reference)
        {
            var result = await _paymentService.VerifyAsync(reference);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _paymentService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
