namespace Admitto.Core.Entities
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public bool IsUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
