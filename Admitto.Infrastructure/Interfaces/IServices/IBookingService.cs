using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Bookings;
using Admitto.Core.Models.Responses.Bookings;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IBookingService
    {
        Task<PagedResponse<BookingResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedResponse<BookingResponse>> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize);
        Task<ApiResponse<BookingResponse>> GetByIdAsync(int id, Guid? callerUserId = null);
        Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request, Guid userId);
        Task<ApiResponse<bool>> CancelAsync(int id, Guid? callerUserId = null);
    }
}
