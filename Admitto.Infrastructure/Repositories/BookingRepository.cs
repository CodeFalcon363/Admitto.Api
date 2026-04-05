using Admitto.Core.Constants;
using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AdmittoDbContext _context;

        public BookingRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetAllByEventSlugAsync(string eventSlug)
        {
            // Single JOIN query — avoids the two-round-trip + large IN-clause pattern.
            return await (
                from b in _context.Bookings
                join bi in _context.BookingItems on b.Id equals bi.BookingId
                join tt in _context.TicketTypes on bi.TicketTypeId equals tt.Id
                join e in _context.Events on tt.EventId equals e.Id
                where e.Slug == eventSlug
                select b
            ).AsNoTracking().Distinct().ToListAsync();
        }

        public Task<Booking?> GetByIdempotencyKeyAsync(string key)
            => _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.IdempotencyKey == key);

        public async Task<(IEnumerable<Booking>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Bookings.CountAsync();
            var data = await _context.Bookings
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<(IEnumerable<Booking>, int totalRecords)> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize)
        {
            var totalCount = await _context.Bookings.CountAsync(b => b.UserId == userId);
            var data = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public Task<Booking?> GetByIdAsync(int id)
            => _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

        public Task<Booking?> GetByIdWithItemsAsync(int id)
            => _context.Bookings
                .AsNoTracking()
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == id);

        /// <summary>
        /// Atomically decrements capacity for every line item, then inserts the booking and its
        /// items — all in a single ReadCommitted transaction. If any capacity decrement fails
        /// (insufficient stock) the transaction is rolled back, restoring all prior decrements.
        /// </summary>
        public async Task<(Booking? booking, string? error)> CreateTransactionalAsync(
            Booking booking, List<BookingLineItem> items)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted);

            try
            {
                // Decrement each ticket type capacity atomically.
                // A single UPDATE that checks Capacity >= quantity prevents overselling.
                // If any returns 0 rows-affected, insufficient stock — roll back everything.
                foreach (var item in items)
                {
                    var rows = await _context.Database.ExecuteSqlAsync(
                        $"UPDATE TicketTypes SET Capacity = Capacity - {item.Quantity} WHERE Id = {item.TicketTypeId} AND Capacity >= {item.Quantity}");

                    if (rows == 0)
                    {
                        await transaction.RollbackAsync();
                        return (null, ApiMessages.InsufficientTickets);
                    }
                }

                // Insert the booking header.
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Insert line items using the DB-generated booking Id.
                var bookingItems = items.Select(i => new BookingItem
                {
                    BookingId = booking.Id,
                    TicketTypeId = i.TicketTypeId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.BookingItems.AddRange(bookingItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                booking.Items = bookingItems;
                return (booking, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Atomically cancels the booking and restores ticket capacity in one ReadCommitted transaction.
        /// Uses a conditional UPDATE (WHERE Status = Confirmed) as the concurrency guard — prevents
        /// two simultaneous cancel requests from restoring capacity twice.
        /// </summary>
        public async Task<(bool success, string? error)> CancelTransactionalAsync(int bookingId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var now           = DateTime.UtcNow;
                var canceledStatus  = (int)BookingStatus.Canceled;
                var confirmedStatus = (int)BookingStatus.Confirmed;

                // Only cancel if currently Confirmed — 0 rows means already canceled or not found.
                var cancelRows = await _context.Database.ExecuteSqlAsync(
                    $"UPDATE Bookings SET Status = {canceledStatus}, UpdatedAt = {now} WHERE Id = {bookingId} AND Status = {confirmedStatus}");

                if (cancelRows == 0)
                {
                    await transaction.RollbackAsync();
                    return (false, ApiMessages.BookingAlreadyCanceled);
                }

                // Restore capacity for every item in this booking.
                var items = await _context.BookingItems
                    .Where(i => i.BookingId == bookingId)
                    .ToListAsync();

                foreach (var item in items)
                {
                    await _context.Database.ExecuteSqlAsync(
                        $"UPDATE TicketTypes SET Capacity = Capacity + {item.Quantity} WHERE Id = {item.TicketTypeId}");
                }

                await transaction.CommitAsync();
                return (true, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Booking?> UpdateAsync(Booking booking)
        {
            // Re-fetch with tracking so EF Core only generates UPDATE statements for
            // columns that actually changed — avoids full-row dirty writes on detached entities.
            var existing = await _context.Bookings.FindAsync(booking.Id);
            if (existing == null) return null;

            _context.Entry(existing).CurrentValues.SetValues(booking);
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdateCount++;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(Booking booking)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
        }
    }
}
