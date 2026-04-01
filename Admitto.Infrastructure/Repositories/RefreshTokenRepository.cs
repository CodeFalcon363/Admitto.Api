using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AdmittoDbContext _context;

        public RefreshTokenRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
            => await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);

        public async Task<RefreshToken?> UpdateAsync(RefreshToken refreshToken)
        {
            var exists = await _context.RefreshTokens.AnyAsync(r => r.Id == refreshToken.Id);
            if (!exists) return null;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        /// <summary>
        /// Atomically marks the token as used only if it is currently unused, unrevoked, and unexpired.
        /// Returns false if another request already consumed it — prevents replay attacks.
        /// </summary>
        public async Task<bool> ConsumeAsync(string token)
        {
            var now = DateTime.UtcNow;
            var rowsAffected = await _context.Database.ExecuteSqlAsync(
                $"UPDATE RefreshTokens SET IsUsed = 1 WHERE Token = {token} AND IsUsed = 0 AND IsRevoked = 0 AND ExpiresAt > {now}");
            return rowsAffected > 0;
        }
    }
}
