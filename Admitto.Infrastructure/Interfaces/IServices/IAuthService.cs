using Admitto.Core.Models;
using Admitto.Core.Models.Requests;
using Admitto.Core.Models.Responses;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<ApiResponse<UserResponse>> RegisterAsync(RegisterUserRequest register);
        Task<ApiResponse<UserResponse>> LoginAsync(LoginRequest logins);
        Task<ApiResponse<UserResponse>> RefreshTokenAsync(string expiredJwt, string refreshToken);
        Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken);

    }
}
