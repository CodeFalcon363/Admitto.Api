using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests;
using Admitto.Core.Models.Responses;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using Admitto.Infrastructure.Repositories;
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
        private readonly ILogger<AuthService> _logger;
        private readonly IMapper _mapper;
        private readonly IRefreshTokenRepository _refreshToken;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUserRepository user, ILogger<AuthService> logger, IMapper mapper, IOptions<JwtSettings> jwtSettings, IRefreshTokenRepository refreshToken)
        {
            _user = user;
            _logger = logger;
            _mapper = mapper;
            _refreshToken = refreshToken;
            _jwtSettings = jwtSettings.Value;
        }
        public async Task<ApiResponse<UserResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _user.GetByEmailAsync(request.Email);
            if (user == null)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.InvalidCredentials
                };

            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!passwordValid)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.InvalidCredentials
                };

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id, token.JwtId);

            await _refreshToken.CreateAsync(refreshToken);

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


        public async Task<ApiResponse<UserResponse>> RefreshTokenAsync(string expiredJwt, string refreshToken)
        {
            var response = await _refreshToken.GetByTokenAsync(refreshToken);
            if (response == null)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.InvalidCredentials
                };
            
            if (response.IsUsed || response.IsRevoked || response.ExpiresAt < DateTime.UtcNow)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.InvalidCredentials
                };
                
            
            if (response.JwtId != GetJwtIdFromToken(expiredJwt))
                return new ApiResponse<UserResponse>
                { 
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };

            response.IsUsed = true;
            await _refreshToken.UpdateAsync(response);

            var user = await _user.GetByIdAsync(response.UserId);
            if (user == null)
                return new ApiResponse<UserResponse>
                { 
                    Success = false,
                    Message = ApiMessages.NotFound
                };

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user.Id, newToken.JwtId);
            await _refreshToken.CreateAsync(newRefreshToken);

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

        public async Task<ApiResponse<UserResponse>> RegisterAsync(RegisterUserRequest register)
        {
            var exists = await _user.AnyAsync(register.Email);
            if (exists)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.UserAlreadyExists
                };

            var user = _mapper.Map<User>(register);
            user.Id = Guid.NewGuid();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(register.Password);
            user.Role = Roles.Attendee;
            user.CreatedAt = DateTime.UtcNow;

            await _user.CreateAsync(user);

            var userResponse = _mapper.Map<UserResponse>(user);

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Message = ApiMessages.RegistrationSuccess,
                Data = userResponse
            };
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
        {
            var storedToken = await _refreshToken.GetByTokenAsync(refreshToken);
            if (storedToken == null)
                return new ApiResponse<bool>
                { 
                    Success = false,
                    Message = ApiMessages.TokenInvalid
                };

            storedToken.IsRevoked = true;
            await _refreshToken.UpdateAsync(storedToken);

            return new ApiResponse<bool>
            { 
                Success = true,
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
