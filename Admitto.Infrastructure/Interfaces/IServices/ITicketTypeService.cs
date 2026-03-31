using Admitto.Core.Models;
using Admitto.Core.Models.Requests.TicketTypes;
using Admitto.Core.Models.Responses.TicketTypes;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface ITicketTypeService
    {
        Task<PagedResponse<TicketTypeResponse>> GetAllByEventSlugAsync(string slug, int pageNumber, int pageSize);
        Task<ApiResponse<TicketTypeResponse>> GetByIdAsync(int id);
        Task<ApiResponse<TicketTypeResponse>> CreateAsync(CreateTicketTypeRequest request);
        Task<ApiResponse<TicketTypeResponse>> UpdateAsync(int id, UpdateTicketTypeRequest request);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
