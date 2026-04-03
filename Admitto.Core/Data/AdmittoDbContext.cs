using Admitto.Core.Models;
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
        public DbSet<Entities.EventMedia> EventMedia { get; set; } = null!;
        public DbSet<Entities.PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<Entities.NotificationRule> NotificationRules { get; set; } = null!;
        public DbSet<Entities.UserNotificationPreference> UserNotificationPreferences { get; set; } = null!;
        public DbSet<Entities.EventReminderOverride> EventReminderOverrides { get; set; } = null!;
        public DbSet<Entities.OrganizerReminderSetting> OrganizerReminderSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entities.BookingItem>()
                .Property(b => b.UnitPrice)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Entities.Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Entities.TicketType>()
                .Property(t => t.Price)
                .HasPrecision(18, 8);

            // Performance indexes — every column here backs a WHERE clause on a hot path.
            // Without these, each lookup is a full table scan.

            // Login and registration duplicate check.
            modelBuilder.Entity<Entities.User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // Every slug-based event lookup + enforces uniqueness across organizers.
            modelBuilder.Entity<Entities.Event>()
                .HasIndex(e => e.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Events_Slug");

            // Refresh token lookup on every token refresh/revoke.
            modelBuilder.Entity<Entities.RefreshToken>()
                .HasIndex(r => r.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            // Password reset token lookup.
            modelBuilder.Entity<Entities.PasswordResetToken>()
                .HasIndex(p => p.Token)
                .IsUnique()
                .HasDatabaseName("IX_PasswordResetTokens_Token");

            // Idempotency key lookup on every booking create.
            modelBuilder.Entity<Entities.Booking>()
                .HasIndex(b => b.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("IX_Bookings_IdempotencyKey");

            // Payment lookup by reference (verify endpoint) and by booking (dedup check).
            modelBuilder.Entity<Entities.Payment>()
                .HasIndex(p => p.PaymentReference)
                .IsUnique()
                .HasDatabaseName("IX_Payments_PaymentReference");

            modelBuilder.Entity<Entities.Payment>()
                .HasIndex(p => p.BookingId)
                .HasDatabaseName("IX_Payments_BookingId");

            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Entities.NotificationRule>().HasData(
                new Entities.NotificationRule { Id = 1, TriggerType = NotificationTrigger.BookingConfirmation, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 2, TriggerType = NotificationTrigger.BookingCancellation, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 3, TriggerType = NotificationTrigger.EventReminder, IsEnabled = true, ReminderHoursBefore = 24, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 4, TriggerType = NotificationTrigger.EventCreated, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 5, TriggerType = NotificationTrigger.EventUpdated, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 6, TriggerType = NotificationTrigger.EventDeleted, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 7, TriggerType = NotificationTrigger.RoleChanged, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate },
                new Entities.NotificationRule { Id = 8, TriggerType = NotificationTrigger.ProfileUpdated, IsEnabled = true, ReminderHoursBefore = null, UpdatedAt = seedDate }
            );
        }
    }
}
