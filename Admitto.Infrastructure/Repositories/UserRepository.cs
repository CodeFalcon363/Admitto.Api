using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AdmittoDbContext _context;

        public UserRepository(AdmittoDbContext context)
        {
            _context = context;
        }
        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<User>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Users.CountAsync();
            var data = await _context.Users
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetByIdAsync(Guid id)
            => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        public async Task<IReadOnlyDictionary<Guid, User>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.Distinct().ToList();
            return await _context.Users
                .AsNoTracking()
                .Where(u => idList.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        public async Task<User?> UpdateAsync(User updated)
        {
            // Re-fetch with tracking so EF Core only generates UPDATE statements for
            // columns that actually changed — avoids full-row dirty writes on detached entities.
            var existing = await _context.Users.FindAsync(updated.Id);
            if (existing == null) return null;

            _context.Entry(existing).CurrentValues.SetValues(updated);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
