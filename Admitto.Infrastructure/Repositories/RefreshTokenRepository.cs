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
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task<RefreshToken?> UpdateAsync(RefreshToken refreshToken)
        {
            var exists = await _context.RefreshTokens.AnyAsync(r => r.Id == refreshToken.Id);
            if (!exists)
                return null;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }
    }
}
