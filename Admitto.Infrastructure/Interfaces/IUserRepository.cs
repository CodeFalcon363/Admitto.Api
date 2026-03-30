using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task<(IEnumerable<User>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(User user);
        Task<bool> AnyAsync(string email);
        Task DeleteAsync(User user);
    }
}
