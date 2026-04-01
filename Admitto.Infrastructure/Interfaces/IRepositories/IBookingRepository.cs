using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IBookingRepository
    {
        Task<(IEnumerable<Booking>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<Booking>, int totalRecords)> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize);
        Task<Booking?> GetByIdAsync(int id);
        Task<Booking?> GetByIdempotencyKeyAsync(string key);
        Task<IEnumerable<Booking>> GetAllByEventSlugAsync(string eventSlug);
        Task<Booking> CreateAsync(Booking booking, List<BookingItem> items);
        Task<Booking?> UpdateAsync(Booking booking);
        Task<bool> AnyAsync(int id);
        Task DeleteAsync(Booking booking);
    }
}
