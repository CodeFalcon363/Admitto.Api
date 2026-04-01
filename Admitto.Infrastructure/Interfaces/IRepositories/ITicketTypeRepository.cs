using Admitto.Core.Entities;
using Admitto.Core.Models;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface ITicketTypeRepository
    {
        Task<(IEnumerable<TicketType>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<TicketType>, int totalRecords)> GetAllByEventIdAsync(int eventId, int pageNumber, int pageSize);
        Task<(IEnumerable<TicketType>, int totalRecords)> GetAllByEventSlugAsync(string eventSlug, int pageNumber, int pageSize);
        Task<TicketType?> GetByIdAsync(int id);
        Task<TicketType> CreateAsync(TicketType ticketType);
        Task<TicketType?> UpdateAsync(TicketType ticketType);
        Task<bool> DecrementCapacityAsync(int ticketTypeId, int quantity);
        Task<bool> AnyAsync(int id);
        Task DeleteAsync(TicketType ticketType);
    }
}
