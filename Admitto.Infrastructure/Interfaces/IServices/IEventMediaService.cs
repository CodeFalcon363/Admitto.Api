using Admitto.Core.Models;
using Admitto.Core.Models.Responses.EventMedia;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IEventMediaService
    {
        Task<ApiResponse<EventMediaResponse>> UploadAsync(int eventId, Stream fileStream, string fileName, MediaType type);
        Task<ApiResponse<IEnumerable<EventMediaResponse>>> GetByEventIdAsync(int eventId);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
