using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Bookings;
using Admitto.Core.Models.Responses.Bookings;

namespace Admitto.Infrastructure.Interfaces
{
    public interface IBookingService
    {
        Task<PagedResponse<BookingResponse>> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize);
        Task<ApiResponse<BookingResponse>> GetByIdAsync(int id);
        Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request);
        Task<ApiResponse<bool>> CancelAsync(int id);
    }
}
