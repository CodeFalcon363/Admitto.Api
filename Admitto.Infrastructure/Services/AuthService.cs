using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests;
using Admitto.Core.Models.Responses;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Data;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Admitto.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _user;
        private readonly IRefreshTokenRepository _refreshToken;
        private readonly IPasswordResetTokenRepository _passwordResetToken;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUserRepository user, IRefreshTokenRepository refreshToken, IPasswordResetTokenRepository passwordResetToken, INotificationService notificationService, IMapper mapper, IOptions<JwtSettings> jwtSettings, ILogger<AuthService> logger)
        {
            _user = user;
            _refreshToken = refreshToken;
            _passwordResetToken = passwordResetToken;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<ApiResponse<UserResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _user.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Log only a truncated identifier — never the raw email — to avoid creating
                // a queryable PII index of all addresses that have been attempted.
                var masked = request.Email.Length > 3
                    ? request.Email[..3] + "***"
                    : "***";
                _logger.LogWarning("Failed login attempt for email {MaskedEmail}", masked);
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.InvalidCredentials
                };
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id, token.JwtId);
            await _refreshToken.CreateAsync(refreshToken);

            _logger.LogInformation("User {UserId} logged in", user.Id);

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Token = token.Value;
            userResponse.RefreshToken = refreshToken.Token;

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Message = ApiMessages.LoginSuccess,
                Data = userResponse
            };
        }

        public async Task<ApiResponse<UserResponse>> RegisterAsync(RegisterUserRequest register)
        {
            var exists = await _user.GetByEmailAsync(register.Email);
            if (exists != null)
            {
                _logger.LogWarning("Registration attempted for existing email {Email}", register.Email);
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.UserAlreadyExists
                };
            }

            var user = _mapper.Map<User>(register);
            user.Id = Guid.NewGuid();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(register.Password);
            user.Role = Roles.Attendee;
            user.CreatedAt = DateTime.UtcNow;

            try
            {
                await _user.CreateAsync(user);
            }
            catch (DbUpdateException ex) when (DbExceptionHelper.IsDuplicateKey(ex))
            {
                // Two concurrent registrations with the same email both passed the pre-check.
                // The unique index on IX_Users_Email rejected the second insert — return the
                // same conflict response instead of letting a 500 reach the client.
                _logger.LogWarning("Concurrent duplicate registration for email — unique index rejected insert");
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.UserAlreadyExists
                };
            }

            _logger.LogInformation("User registered: {UserId}", user.Id);

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Message = ApiMessages.RegistrationSuccess,
                Data = _mapper.Map<UserResponse>(user)
            };
        }

        public async Task<ApiResponse<UserResponse>> RefreshTokenAsync(string expiredJwt, string refreshToken)
        {
            var storedToken = await _refreshToken.GetByTokenAsync(refreshToken);
            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token used");
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.InvalidCredentials
                };
            }

            if (storedToken.JwtId != GetJwtIdFromToken(expiredJwt))
            {
                _logger.LogWarning("Refresh token JwtId mismatch for token {TokenId}", storedToken.Id);
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };
            }

            // Atomic consume — only the first concurrent request wins.
            // The SQL UPDATE checks IsUsed = 0 in the same statement.
            var consumed = await _refreshToken.ConsumeAsync(refreshToken);
            if (!consumed)
            {
                _logger.LogWarning("Refresh token replay attempt detected for token {TokenId}", storedToken.Id);
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };
            }

            var user = await _user.GetByIdAsync(storedToken.UserId);
            if (user == null)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.NotFound
                };

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user.Id, newToken.JwtId);
            await _refreshToken.CreateAsync(newRefreshToken);

            _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Token = newToken.Value;
            userResponse.RefreshToken = newRefreshToken.Token;

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Message = ApiMessages.LoginSuccess,
                Data = userResponse
            };
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
        {
            var storedToken = await _refreshToken.GetByTokenAsync(refreshToken);
            if (storedToken == null)
            {
                _logger.LogWarning("Attempted to revoke non-existent refresh token");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };
            }

            storedToken.IsRevoked = true;
            await _refreshToken.UpdateAsync(storedToken);

            _logger.LogInformation("Refresh token revoked for user {UserId}", storedToken.UserId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true
            };
        }

        public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
        {
            var user = await _user.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for unknown email");
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = ApiMessages.PasswordResetEmailSent,
                    Data = true
                };
            }

            var token = GenerateBase64UrlToken(32);
            await _passwordResetToken.CreateAsync(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await _notificationService.SendPasswordResetAsync(user.Email, user.FirstName, token);

            _logger.LogInformation("Password reset token issued for user {UserId}", user.Id);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.PasswordResetEmailSent,
                Data = true
            };
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            var resetToken = await _passwordResetToken.GetByTokenAsync(token);
            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired password reset token used");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };
            }

            // Atomic consume — only the first concurrent request wins.
            // The SQL UPDATE checks IsUsed = 0 in the same statement.
            var consumed = await _passwordResetToken.ConsumeAsync(token);
            if (!consumed)
            {
                _logger.LogWarning("Password reset token replay attempt detected");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };
            }

            var user = await _user.GetByIdAsync(resetToken.UserId);
            if (user == null)
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.NotFound
                };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _user.UpdateAsync(user);

            _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.PasswordResetSuccess,
                Data = true
            };
        }

        private (string Value, string JwtId) GenerateJwtToken(User user)
        {
            var jwtId = Guid.NewGuid().ToString();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), jwtId);
        }

        private RefreshToken GenerateRefreshToken(Guid userId, string jwtId)
        {
            return new RefreshToken
            {
                UserId = userId,
                JwtId = jwtId,
                Token = GenerateBase64UrlToken(64),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            };
        }

        /// <summary>
        /// Produces a URL-safe Base64 token (RFC 4648 §5).
        /// Standard Base64 (+, /, =) breaks URL query parameters without explicit encoding.
        /// </summary>
        private static string GenerateBase64UrlToken(int byteLength)
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

        /// <summary>
        /// Validates the expired JWT's signature, issuer, and audience — but NOT its lifetime,
        /// since it is expected to be expired. Extracting the JTI from an unvalidated token
        /// would allow any attacker who knows the stored JTI to forge a matching JWT.
        /// </summary>
        private string? GetJwtIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // token is expected to be expired
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = handler.ValidateToken(token, validationParams, out _);
                return principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Expired JWT failed signature/issuer validation during token refresh");
                return null;
            }
        }
    }
}
