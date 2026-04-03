using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class NotificationRuleRepository : INotificationRuleRepository
    {
        private readonly AdmittoDbContext _context;

        public NotificationRuleRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationRule>> GetAllAsync()
            => await _context.NotificationRules.AsNoTracking().ToListAsync();

        public Task<NotificationRule?> GetByIdAsync(int id)
            => _context.NotificationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        public Task<NotificationRule?> GetByTriggerAsync(NotificationTrigger trigger)
            => _context.NotificationRules.AsNoTracking().FirstOrDefaultAsync(r => r.TriggerType == trigger);

        public async Task UpdateAsync(NotificationRule rule)
        {
            _context.NotificationRules.Update(rule);
            await _context.SaveChangesAsync();
        }
    }
}
