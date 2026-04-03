using Admitto.Core.Entities;
using Admitto.Core.Models;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IBookingRepository
    {
        Task<(IEnumerable<Booking>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<Booking>, int totalRecords)> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize);
        Task<Booking?> GetByIdAsync(int id);
        Task<Booking?> GetByIdWithItemsAsync(int id);
        Task<Booking?> GetByIdempotencyKeyAsync(string key);
        Task<IEnumerable<Booking>> GetAllByEventSlugAsync(string eventSlug);
        /// <summary>
        /// Atomically decrements capacity for all items, inserts booking and items in one transaction.
        /// Rolls back all capacity decrements if any item has insufficient stock.
        /// Returns (booking, null) on success, (null, errorMessage) on failure.
        /// </summary>
        Task<(Booking? booking, string? error)> CreateTransactionalAsync(Booking booking, List<BookingLineItem> items);
        Task<Booking?> UpdateAsync(Booking booking);
        Task DeleteAsync(Booking booking);
    }
}
