using Microsoft.EntityFrameworkCore;

namespace Admitto.Core.Data
{
    public class AdmittoDbContext : DbContext
    {
        public AdmittoDbContext(DbContextOptions<AdmittoDbContext> context) : base(context)
        {
            
        }
        public DbSet<Entities.User> Users { get; set; } = null!;
        public DbSet<Entities.Event> Events { get; set; } = null!;
        public DbSet<Entities.Booking> Bookings { get; set; } = null!;
        public DbSet<Entities.TicketType> TicketTypes { get; set; } = null!;
        public DbSet<Entities.BookingItem> BookingItems { get; set; } = null!;
        public DbSet<Entities.Payment> Payments { get; set; } = null!;
        public DbSet<Entities.RefreshToken> RefreshTokens { get; set; } = null!;

    }
}
