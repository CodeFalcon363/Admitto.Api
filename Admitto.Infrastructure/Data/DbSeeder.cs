using Admitto.Core.Constants;
using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admitto.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AdmittoDbContext>();
            var settings = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AdmittoDbContext>>();

            if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
            {
                logger.LogDebug("AdminSeedSettings not configured — skipping admin seed");
                return;
            }

            var exists = await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == settings.Email && u.Role == Roles.Admin);

            if (exists)
            {
                logger.LogDebug("Admin account already exists — skipping seed");
                return;
            }

            var admin = new User
            {
                Id = Guid.NewGuid(),
                FirstName = settings.FirstName,
                LastName = settings.LastName,
                Email = settings.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.Password),
                Role = Roles.Admin,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            logger.LogInformation("Admin account seeded for {Email}", settings.Email);
        }
    }
}
