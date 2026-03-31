using Admitto.Core.Attributes;

namespace Admitto.Core.Models.Requests
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;

        [Sensitive]
        public string Password { get; set; } = null!;

        public override string ToString() => $"Email: {Email}, Password: [REDACTED]";
    }
}