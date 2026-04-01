using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AdmittoDbContext _context;

        public EventRepository(AdmittoDbContext context)
        {
            _context = context;
        }
        public async Task<bool> AnyAsync(int id)
        {
            var response = await _context.Events.AnyAsync(e => e.Id == id);
            return response;
        }

        public async Task<Event> CreateAsync(Event e)
        {
            await _context.Events.AddAsync(e);
            await _context.SaveChangesAsync();
            return e;
        }

        public async Task DeleteAsync(Event e)
        {
            var response = await _context.Events.AnyAsync(u => u.Id == e.Id);
            if (response)
            {
                _context.Events.Remove(e);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<Event>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Events.CountAsync();
            var data = await _context.Events
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            var response = await _context.Events.FindAsync(id);
            return response;
        }

        public Task<Event?> GetBySlugAsync(string slug)
        {
            var response = _context.Events.FirstOrDefaultAsync(s => s.Slug == slug);
            return response;
        }

        public async Task<Event?> UpdateAsync(Event e)
        {
            var response = await _context.Events.AnyAsync(ev => ev.Id == e.Id);
            if (response == false)
                return null;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdateCount++;
            _context.Events.Update(e);
            await _context.SaveChangesAsync();
            return e;
        }
    }
}
