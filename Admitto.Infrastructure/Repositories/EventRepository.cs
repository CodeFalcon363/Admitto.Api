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
        public async Task<Event> CreateAsync(Event e)
        {
            await _context.Events.AddAsync(e);
            await _context.SaveChangesAsync();
            return e;
        }

        public async Task DeleteAsync(Event e)
        {
            _context.Events.Remove(e);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<Event>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Events.CountAsync();
            var data = await _context.Events
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public Task<Event?> GetByIdAsync(int id)
            => _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        public Task<Event?> GetBySlugAsync(string slug)
            => _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug);

        public Task<bool> SlugExistsAsync(string slug)
            => _context.Events.AnyAsync(e => e.Slug == slug);

        public async Task<Event?> UpdateAsync(Event e)
        {
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdateCount++;
            _context.Events.Update(e);
            await _context.SaveChangesAsync();
            return e;
        }
    }
}
