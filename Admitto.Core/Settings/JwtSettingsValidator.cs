using Microsoft.Extensions.Options;

namespace Admitto.Core.Settings
{
    /// <summary>
    /// Validates JwtSettings at startup. HMAC-SHA256 requires a minimum 256-bit (32-byte) key.
    /// A shorter key produces a weak or invalid HMAC signature without a runtime error until the
    /// first token operation — catching it here fails fast with a clear message.
    /// </summary>
    public class JwtSettingsValidator : IValidateOptions<JwtSettings>
    {
        public ValidateOptionsResult Validate(string? name, JwtSettings options)
        {
            if (string.IsNullOrWhiteSpace(options.SecretKey) || options.SecretKey.Length < 32)
                return ValidateOptionsResult.Fail(
                    "JwtSettings.SecretKey must be at least 32 characters (256 bits) for HMAC-SHA256.");

            if (string.IsNullOrWhiteSpace(options.Issuer))
                return ValidateOptionsResult.Fail("JwtSettings.Issuer is required.");

            if (string.IsNullOrWhiteSpace(options.Audience))
                return ValidateOptionsResult.Fail("JwtSettings.Audience is required.");

            if (options.ExpiryMinutes <= 0)
                return ValidateOptionsResult.Fail("JwtSettings.ExpiryMinutes must be greater than 0.");

            if (options.RefreshTokenExpiryDays <= 0)
                return ValidateOptionsResult.Fail("JwtSettings.RefreshTokenExpiryDays must be greater than 0.");

            return ValidateOptionsResult.Success;
        }
    }
}
