using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IEventReminderOverrideRepository
    {
        Task<EventReminderOverride?> GetByEventAsync(int eventId);
        Task UpsertAsync(EventReminderOverride eventOverride);
    }
}
