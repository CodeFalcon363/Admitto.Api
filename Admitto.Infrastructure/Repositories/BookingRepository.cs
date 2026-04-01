using Admitto.Core.Data;
using Admitto.Core.Entities;
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
        {
            var response = await _context.Bookings.AnyAsync(b => b.Id == id);
            return response;
        }

        public async Task<IEnumerable<Booking>> GetAllByEventSlugAsync(string eventSlug)
        {
            var bookingIds = await (
                from bi in _context.BookingItems
                join tt in _context.TicketTypes on bi.TicketTypeId equals tt.Id
                join e in _context.Events on tt.EventId equals e.Id
                where e.Slug == eventSlug
                select bi.BookingId
            ).Distinct().ToListAsync();

            return await _context.Bookings
                .Where(b => bookingIds.Contains(b.Id))
                .ToListAsync();
        }

        public async Task<Booking?> GetByIdempotencyKeyAsync(string key)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.IdempotencyKey == key);
        }

        public async Task<Booking> CreateAsync(Booking booking, List<BookingItem> items)
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            foreach (var item in items)
            {
                item.BookingId = booking.Id;
            }
            await _context.BookingItems.AddRangeAsync(items);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task DeleteAsync(Booking booking)
        {
            var response = await _context.Bookings.AnyAsync(e => e.Id == booking.Id);
            if(response)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }

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
            var totalCount = await _context.Bookings.CountAsync(e => e.UserId == userId);
            var data = await _context.Bookings
                .Where(e => e.UserId == userId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            var response = await _context.Bookings.FindAsync(id);
            if (response == null)
                return null;
            return response;
        }

        public async Task<Booking?> UpdateAsync(Booking booking)
        {
            var response = await _context.Bookings.AnyAsync(e => e.Id == booking.Id);
            if (response == false)
                return null;
            booking.UpdatedAt = DateTime.UtcNow;
            booking.UpdateCount++;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return booking;
        }
    }
}
