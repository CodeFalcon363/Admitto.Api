using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AdmittoDbContext _context;

        public PasswordResetTokenRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
            => await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);

        public async Task UpdateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Atomically marks the token as used only if it is currently unused and unexpired.
        /// Returns false if another request already consumed it — prevents reset token replay.
        /// </summary>
        public async Task<bool> ConsumeAsync(string token)
        {
            var now = DateTime.UtcNow;
            var rowsAffected = await _context.Database.ExecuteSqlAsync(
                $"UPDATE PasswordResetTokens SET IsUsed = 1 WHERE Token = {token} AND IsUsed = 0 AND ExpiresAt > {now}");
            return rowsAffected > 0;
        }
    }
}
