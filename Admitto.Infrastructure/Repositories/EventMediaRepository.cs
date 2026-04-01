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

        public async Task<EventMedia?> GetByIdAsync(int id)
        {
            return await _context.EventMedia.FindAsync(id);
        }

        public async Task<IEnumerable<EventMedia>> GetAllByEventIdAsync(int eventId)
        {
            return await _context.EventMedia.Where(m => m.EventId == eventId).ToListAsync();
        }

        public async Task DeleteAsync(EventMedia media)
        {
            _context.EventMedia.Remove(media);
            await _context.SaveChangesAsync();
        }
    }
}
