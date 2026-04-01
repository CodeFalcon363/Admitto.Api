using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Users;
using Admitto.Core.Models.Responses;

namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IUserService
    {
        Task<ApiResponse<UserResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<UserResponse>> UpdateProfileAsync(Guid userId, UpdateUserRequest request);
        Task<ApiResponse<bool>> ChangeRoleAsync(Guid userId, ChangeRoleRequest request);
    }
}
