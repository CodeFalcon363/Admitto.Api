using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Payments;
using Admitto.Core.Models.Responses.Payments;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IPaymentService
    {
        Task<ApiResponse<PaymentResponse>> InitializeAsync(InitializePaymentRequest request, Guid callerUserId);
        Task<ApiResponse<PaymentResponse>> VerifyAsync(string reference, Guid callerUserId, bool isAdmin);
        Task<ApiResponse<PaymentResponse>> GetByIdAsync(int id, Guid callerUserId, bool isAdmin);
    }
}
