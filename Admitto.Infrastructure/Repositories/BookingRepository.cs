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

        public async Task<bool> AnyAsync(int id)
            => await _context.Bookings.AnyAsync(b => b.Id == id);

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
            ).Distinct().ToListAsync();
        }

        public async Task<Booking?> GetByIdempotencyKeyAsync(string key)
            => await _context.Bookings.FirstOrDefaultAsync(b => b.IdempotencyKey == key);

        public async Task<(IEnumerable<Booking>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Bookings.CountAsync();
            var data = await _context.Bookings
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<(IEnumerable<Booking>, int totalRecords)> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize)
        {
            var totalCount = await _context.Bookings.CountAsync(b => b.UserId == userId);
            var data = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<Booking?> GetByIdAsync(int id)
            => await _context.Bookings.FindAsync(id);

        public async Task<Booking?> GetByIdWithItemsAsync(int id)
            => await _context.Bookings
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

        public async Task<Booking?> UpdateAsync(Booking booking)
        {
            booking.UpdatedAt = DateTime.UtcNow;
            booking.UpdateCount++;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task DeleteAsync(Booking booking)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
        }
    }
}
