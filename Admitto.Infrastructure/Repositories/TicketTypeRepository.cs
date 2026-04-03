using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class TicketTypeRepository : ITicketTypeRepository
    {
        private readonly AdmittoDbContext _context;

        public TicketTypeRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AnyAsync(int id)
            => await _context.TicketTypes.AnyAsync(e => e.Id == id);

        public async Task<TicketType> CreateAsync(TicketType ticketType)
        {
            await _context.TicketTypes.AddAsync(ticketType);
            await _context.SaveChangesAsync();
            return ticketType;
        }

        public async Task DeleteAsync(TicketType ticketType)
        {
            _context.TicketTypes.Remove(ticketType);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<TicketType>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.TicketTypes.CountAsync();
            var data = await _context.TicketTypes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<(IEnumerable<TicketType>, int totalRecords)> GetAllByEventIdAsync(int eventId, int pageNumber, int pageSize)
        {
            var totalCount = await _context.TicketTypes.CountAsync(e => e.EventId == eventId);
            var data = await _context.TicketTypes
                .Where(e => e.EventId == eventId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<(IEnumerable<TicketType>, int totalRecords)> GetAllByEventSlugAsync(string eventSlug, int pageNumber, int pageSize)
        {
            var query = from t in _context.TicketTypes
                        join e in _context.Events on t.EventId equals e.Id
                        where e.Slug == eventSlug
                        select t;
            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<TicketType?> GetByIdAsync(int id)
            => await _context.TicketTypes.FindAsync(id);

        public async Task<TicketType?> UpdateAsync(TicketType ticketType)
        {
            ticketType.UpdatedAt = DateTime.UtcNow;
            ticketType.UpdateCount++;
            _context.TicketTypes.Update(ticketType);
            await _context.SaveChangesAsync();
            return ticketType;
        }

        /// <summary>
        /// Atomically decrements capacity only if sufficient stock exists.
        /// Returns false if capacity is insufficient — prevents overselling under concurrent load.
        /// </summary>
        public async Task<bool> DecrementCapacityAsync(int ticketTypeId, int quantity)
        {
            var rowsAffected = await _context.Database.ExecuteSqlAsync(
                $"UPDATE TicketTypes SET Capacity = Capacity - {quantity} WHERE Id = {ticketTypeId} AND Capacity >= {quantity}");
            return rowsAffected > 0;
        }
    }
}
