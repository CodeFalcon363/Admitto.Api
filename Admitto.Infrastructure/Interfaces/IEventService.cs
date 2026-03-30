using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Events;
using Admitto.Core.Models.Responses.Events;

namespace Admitto.Infrastructure.Interfaces
{
    public interface IEventService
    {
        Task<PagedResponse<EventResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ApiResponse<EventResponse>> GetByIdAsync(int id);
        Task<ApiResponse<EventResponse>> GetBySlugAsync(string slug);
        Task<ApiResponse<EventResponse>> CreateAsync(CreateEventRequest request);
        Task<ApiResponse<EventResponse>> UpdateAsync(string slug, UpdateEventRequest request);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
