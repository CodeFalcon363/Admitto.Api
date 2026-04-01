using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IEventMediaRepository
    {
        Task<EventMedia> CreateAsync(EventMedia media);
        Task<EventMedia?> GetByIdAsync(int id);
        Task<IEnumerable<EventMedia>> GetAllByEventIdAsync(int eventId);
        Task DeleteAsync(EventMedia media);
    }
}
