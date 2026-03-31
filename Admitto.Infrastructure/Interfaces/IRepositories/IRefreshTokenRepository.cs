using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces.IRepositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> UpdateAsync(RefreshToken refreshToken);
    }
}
