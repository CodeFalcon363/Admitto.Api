using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Admitto.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AdmittoDbContext _context;

        public PasswordResetTokenRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Stores the SHA-256 hash of the token, not the raw token.
        /// If the DB is breached, the attacker gets hashes — useless without the raw values
        /// that were sent to users and are never persisted.
        /// </summary>
        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
        {
            token.Token = Hash(token.Token);
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        /// <summary>
        /// Hashes the caller-supplied raw token before querying — matches the stored hash.
        /// </summary>
        public Task<PasswordResetToken?> GetByTokenAsync(string token)
            => _context.PasswordResetTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Token == Hash(token));

        public async Task UpdateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Atomically marks the token as used only if it is currently unused and unexpired.
        /// The WHERE clause matches against the stored hash of the token.
        /// Returns false if another request already consumed it — prevents reset token replay.
        /// </summary>
        public async Task<bool> ConsumeAsync(string token)
        {
            var hash = Hash(token);
            var now = DateTime.UtcNow;
            var rowsAffected = await _context.Database.ExecuteSqlAsync(
                $"UPDATE PasswordResetTokens SET IsUsed = 1 WHERE Token = {hash} AND IsUsed = 0 AND ExpiresAt > {now}");
            return rowsAffected > 0;
        }

        /// <summary>
        /// SHA-256 hex of the input. Deterministic, one-way, collision-resistant.
        /// Hex encoding avoids any Base64 padding/charset issues in the DB column.
        /// </summary>
        private static string Hash(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
