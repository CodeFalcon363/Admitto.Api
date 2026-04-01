using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IOrganizerReminderSettingRepository
    {
        Task<OrganizerReminderSetting?> GetByOrganizerAsync(Guid organizerId);
        Task UpsertAsync(OrganizerReminderSetting setting);
    }
}
