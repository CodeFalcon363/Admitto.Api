using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces
{
    public interface IBookingRepository
    {
        Task<(IEnumerable<Booking>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<Booking>, int totalRecords)> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize);
        Task<Booking?> GetByIdAsync(int id);
        Task<Booking> CreateAsync(Booking booking);
        Task<Booking?> UpdateAsync(Booking booking);
        Task<bool> AnyAsync(int id);
        Task DeleteAsync(Booking booking);
    }
}
