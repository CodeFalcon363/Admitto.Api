using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class EventMediaRepository : IEventMediaRepository
    {
        private readonly AdmittoDbContext _context;

        public EventMediaRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<EventMedia> CreateAsync(EventMedia media)
        {
            await _context.EventMedia.AddAsync(media);
            await _context.SaveChangesAsync();
            return media;
        }

        public Task<EventMedia?> GetByIdAsync(int id)
            => _context.EventMedia.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

        public async Task<IEnumerable<EventMedia>> GetAllByEventIdAsync(int eventId)
            => await _context.EventMedia
                .AsNoTracking()
                .Where(m => m.EventId == eventId)
                .ToListAsync();

        public async Task DeleteAsync(EventMedia media)
        {
            _context.EventMedia.Remove(media);
            await _context.SaveChangesAsync();
        }
    }
}
