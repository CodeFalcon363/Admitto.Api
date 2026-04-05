using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IOutboxRepository
    {
        Task EnqueueAsync(string eventType, string payload);
        Task<List<OutboxMessage>> GetPendingAsync(int batchSize = 50);
        Task MarkProcessedAsync(int id);
        Task MarkFailedAsync(int id, string error);
    }
}
