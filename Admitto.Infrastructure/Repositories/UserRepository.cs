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
        public async Task<bool> AnyAsync(string email)
        {
            var response = await _context.Users.AnyAsync(u => u.Email == email);
            return response;
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
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);

        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var response = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if(response == null)
                return null;
            return response;
        }

        public async Task<User?> GetByIdAsync(Guid id)
            => await _context.Users.FindAsync(id);

        public async Task<IReadOnlyDictionary<Guid, User>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.Distinct().ToList();
            return await _context.Users
                .Where(u => idList.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        public async Task<User?> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
