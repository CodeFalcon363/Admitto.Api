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
            await _context.Database.ExecuteSqlAsync(
                $"""
                MERGE INTO EventReminderOverrides WITH (HOLDLOCK) AS target
                USING (VALUES ({eventOverride.EventId})) AS source (EventId)
                ON target.EventId = source.EventId
                WHEN MATCHED THEN
                    UPDATE SET ReminderHoursBefore = {eventOverride.ReminderHoursBefore}, UpdatedAt = {eventOverride.UpdatedAt}
                WHEN NOT MATCHED THEN
                    INSERT (EventId, ReminderHoursBefore, UpdatedAt)
                    VALUES ({eventOverride.EventId}, {eventOverride.ReminderHoursBefore}, {eventOverride.UpdatedAt});
                """);
        }
    }
}
