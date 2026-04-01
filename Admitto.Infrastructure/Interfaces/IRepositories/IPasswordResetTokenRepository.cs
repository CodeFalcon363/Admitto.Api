using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken> CreateAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetByTokenAsync(string token);
        Task UpdateAsync(PasswordResetToken token);
        Task<bool> ConsumeAsync(string token);
    }
}
