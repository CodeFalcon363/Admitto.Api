using Admitto.Core.Models;
using Admitto.Core.Models.Responses.Events;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IEventDiscoveryService
    {
        Task<(PagedResponse<IEnumerable<ExternalEventResponse>>, int totalRecords)> SearchEventsAsync(string query, int pageNumber, int pageSize);
    }
}
