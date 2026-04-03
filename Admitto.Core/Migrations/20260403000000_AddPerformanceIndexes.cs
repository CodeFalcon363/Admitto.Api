using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admitto.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users.Email — login and registration duplicate check (full table scan without this).
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // Events.Slug — every slug-based endpoint + enforces slug uniqueness.
            migrationBuilder.CreateIndex(
                name: "IX_Events_Slug",
                table: "Events",
                column: "Slug",
                unique: true);

            // RefreshTokens.Token — looked up on every token refresh and revoke.
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            // PasswordResetTokens.Token — looked up on every password reset attempt.
            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            // Bookings.IdempotencyKey — checked on every booking creation.
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_IdempotencyKey",
                table: "Bookings",
                column: "IdempotencyKey",
                unique: true);

            // Payments.PaymentReference — verify endpoint lookup.
            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentReference",
                table: "Payments",
                column: "PaymentReference",
                unique: true);

            // Payments.BookingId — payment deduplication check in GetOrCreateAsync.
            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_Email",               table: "Users");
            migrationBuilder.DropIndex(name: "IX_Events_Slug",               table: "Events");
            migrationBuilder.DropIndex(name: "IX_RefreshTokens_Token",       table: "RefreshTokens");
            migrationBuilder.DropIndex(name: "IX_PasswordResetTokens_Token", table: "PasswordResetTokens");
            migrationBuilder.DropIndex(name: "IX_Bookings_IdempotencyKey",   table: "Bookings");
            migrationBuilder.DropIndex(name: "IX_Payments_PaymentReference", table: "Payments");
            migrationBuilder.DropIndex(name: "IX_Payments_BookingId",        table: "Payments");
        }
    }
}
