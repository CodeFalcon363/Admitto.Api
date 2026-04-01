using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests;
using Admitto.Core.Models.Responses;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
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
            if (user == null)
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.InvalidCredentials };

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.InvalidCredentials };

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id, token.JwtId);
            await _refreshToken.CreateAsync(refreshToken);

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Token = token.Value;
            userResponse.RefreshToken = refreshToken.Token;

            return new ApiResponse<UserResponse> { Success = true, Message = ApiMessages.LoginSuccess, Data = userResponse };
        }

        public async Task<ApiResponse<UserResponse>> RegisterAsync(RegisterUserRequest register)
        {
            var exists = await _user.AnyAsync(register.Email);
            if (exists)
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.UserAlreadyExists };

            var user = _mapper.Map<User>(register);
            user.Id = Guid.NewGuid();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(register.Password);
            user.Role = Roles.Attendee;
            user.CreatedAt = DateTime.UtcNow;

            await _user.CreateAsync(user);

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Message = ApiMessages.RegistrationSuccess,
                Data = _mapper.Map<UserResponse>(user)
            };
        }

        public async Task<ApiResponse<UserResponse>> RefreshTokenAsync(string expiredJwt, string refreshToken)
        {
            var response = await _refreshToken.GetByTokenAsync(refreshToken);
            if (response == null)
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.InvalidCredentials };

            if (response.IsUsed || response.IsRevoked || response.ExpiresAt < DateTime.UtcNow)
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.InvalidCredentials };

            if (response.JwtId != GetJwtIdFromToken(expiredJwt))
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.TokenInvalid };

            response.IsUsed = true;
            await _refreshToken.UpdateAsync(response);

            var user = await _user.GetByIdAsync(response.UserId);
            if (user == null)
                return new ApiResponse<UserResponse> { Success = false, Message = ApiMessages.NotFound };

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user.Id, newToken.JwtId);
            await _refreshToken.CreateAsync(newRefreshToken);

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Token = newToken.Value;
            userResponse.RefreshToken = newRefreshToken.Token;

            return new ApiResponse<UserResponse> { Success = true, Message = ApiMessages.LoginSuccess, Data = userResponse };
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
        {
            var storedToken = await _refreshToken.GetByTokenAsync(refreshToken);
            if (storedToken == null)
                return new ApiResponse<bool> { Success = false, Message = ApiMessages.TokenInvalid };

            storedToken.IsRevoked = true;
            await _refreshToken.UpdateAsync(storedToken);

            return new ApiResponse<bool> { Success = true, Data = true };
        }

        public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
        {
            var user = await _user.GetByEmailAsync(email);
            // Don't reveal whether the email exists — always return success
            if (user == null)
                return new ApiResponse<bool> { Success = true, Message = ApiMessages.PasswordResetEmailSent, Data = true };

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await _passwordResetToken.CreateAsync(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await _notificationService.SendPasswordResetAsync(user.Email, user.FirstName, token);
            return new ApiResponse<bool> { Success = true, Message = ApiMessages.PasswordResetEmailSent, Data = true };
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            var resetToken = await _passwordResetToken.GetByTokenAsync(token);
            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
                return new ApiResponse<bool> { Success = false, Message = ApiMessages.TokenInvalid };

            var user = await _user.GetByIdAsync(resetToken.UserId);
            if (user == null)
                return new ApiResponse<bool> { Success = false, Message = ApiMessages.NotFound };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _user.UpdateAsync(user);

            resetToken.IsUsed = true;
            await _passwordResetToken.UpdateAsync(resetToken);

            return new ApiResponse<bool> { Success = true, Message = ApiMessages.PasswordResetSuccess, Data = true };
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
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            };
        }

        private string? GetJwtIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
    }
}
