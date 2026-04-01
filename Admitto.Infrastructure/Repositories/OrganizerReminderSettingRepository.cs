using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class OrganizerReminderSettingRepository : IOrganizerReminderSettingRepository
    {
        private readonly AdmittoDbContext _context;

        public OrganizerReminderSettingRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<OrganizerReminderSetting?> GetByOrganizerAsync(Guid organizerId)
            => await _context.OrganizerReminderSettings.FirstOrDefaultAsync(s => s.OrganizerId == organizerId);

        public async Task UpsertAsync(OrganizerReminderSetting setting)
        {
            await _context.Database.ExecuteSqlAsync(
                $"""
                MERGE INTO OrganizerReminderSettings WITH (HOLDLOCK) AS target
                USING (VALUES ({setting.OrganizerId})) AS source (OrganizerId)
                ON target.OrganizerId = source.OrganizerId
                WHEN MATCHED THEN
                    UPDATE SET ReminderHoursBefore = {setting.ReminderHoursBefore}, UpdatedAt = {setting.UpdatedAt}
                WHEN NOT MATCHED THEN
                    INSERT (OrganizerId, ReminderHoursBefore, UpdatedAt)
                    VALUES ({setting.OrganizerId}, {setting.ReminderHoursBefore}, {setting.UpdatedAt});
                """);
        }
    }
}
