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
            var existing = await _context.OrganizerReminderSettings
                .FirstOrDefaultAsync(s => s.OrganizerId == setting.OrganizerId);

            if (existing == null)
                _context.OrganizerReminderSettings.Add(setting);
            else
            {
                existing.ReminderHoursBefore = setting.ReminderHoursBefore;
                existing.UpdatedAt = setting.UpdatedAt;
            }

            await _context.SaveChangesAsync();
        }
    }
}
