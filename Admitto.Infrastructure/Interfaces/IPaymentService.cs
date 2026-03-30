using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Payments;
using Admitto.Core.Models.Responses.Payments;

namespace Admitto.Infrastructure.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<PaymentResponse>> InitializeAsync(InitializePaymentRequest request);
        Task<ApiResponse<PaymentResponse>> VerifyAsync(string reference);
        Task<ApiResponse<PaymentResponse>> GetByIdAsync(int id);
    }
}
