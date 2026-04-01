using Admitto.Core.Attributes;

namespace Admitto.Core.Models.Requests.Auth
{
    public class ResetPasswordRequest
    {
        public string Token { get; set; } = null!;

        [Sensitive]
        public string NewPassword { get; set; } = null!;
    }
}
