using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class EventReminderOverrideRepository : IEventReminderOverrideRepository
    {
        private readonly AdmittoDbContext _context;

        public EventReminderOverrideRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<EventReminderOverride?> GetByEventAsync(int eventId)
            => await _context.EventReminderOverrides.FirstOrDefaultAsync(o => o.EventId == eventId);

        public async Task UpsertAsync(EventReminderOverride eventOverride)
        {
            var existing = await _context.EventReminderOverrides
                .FirstOrDefaultAsync(o => o.EventId == eventOverride.EventId);

            if (existing == null)
                _context.EventReminderOverrides.Add(eventOverride);
            else
            {
                existing.ReminderHoursBefore = eventOverride.ReminderHoursBefore;
                existing.UpdatedAt = eventOverride.UpdatedAt;
            }

            await _context.SaveChangesAsync();
        }
    }
}
