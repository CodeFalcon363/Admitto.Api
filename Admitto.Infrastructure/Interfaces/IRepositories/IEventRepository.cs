using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IEventRepository
    {
        Task<(IEnumerable<Event>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<Event?> GetByIdAsync(int id);
        Task<Event?> GetBySlugAsync(string slug);
        Task<bool> SlugExistsAsync(string slug);
        Task<Event> CreateAsync(Event e);
        Task<Event?> UpdateAsync(Event e);
        Task<bool> AnyAsync(int id);
        Task DeleteAsync(Event e);
    }
}
