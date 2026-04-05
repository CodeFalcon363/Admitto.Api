using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private const int MaxRetries = 3;

        private readonly AdmittoDbContext _context;

        public OutboxRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task EnqueueAsync(string eventType, string payload)
        {
            _context.OutboxMessages.Add(new OutboxMessage
            {
                EventType = eventType,
                Payload   = payload,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns up to <paramref name="batchSize"/> messages that have never been processed
        /// and have not yet exhausted their retry budget.
        /// </summary>
        public Task<List<OutboxMessage>> GetPendingAsync(int batchSize = 50)
            => _context.OutboxMessages
                .AsNoTracking()
                .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();

        public async Task MarkProcessedAsync(int id)
        {
            var now = DateTime.UtcNow;
            await _context.Database.ExecuteSqlAsync(
                $"UPDATE OutboxMessages SET ProcessedAt = {now} WHERE Id = {id}");
        }

        public async Task MarkFailedAsync(int id, string error)
        {
            var truncated = error.Length > 1000 ? error[..1000] : error;
            await _context.Database.ExecuteSqlAsync(
                $"UPDATE OutboxMessages SET RetryCount = RetryCount + 1, Error = {truncated} WHERE Id = {id}");
        }
    }
}
