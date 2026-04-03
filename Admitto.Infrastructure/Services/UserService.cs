using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Users;
using Admitto.Core.Models.Responses;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, INotificationService notificationService, IMapper mapper, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<UserResponse>> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.UserNotFound
                };

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Data = _mapper.Map<UserResponse>(user)
            };
        }

        public async Task<ApiResponse<UserResponse>> UpdateProfileAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = ApiMessages.UserNotFound
                };

            _mapper.Map(request, user);
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Profile updated for user {UserId}", userId);
            _ = Task.Run(() => _notificationService.SendProfileUpdatedAsync(userId));

            return new ApiResponse<UserResponse>
            {
                Success = true,
                Message = ApiMessages.ProfileUpdated,
                Data = _mapper.Map<UserResponse>(user)
            };
        }

        public async Task<ApiResponse<bool>> ChangeRoleAsync(Guid userId, ChangeRoleRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.UserNotFound
                };

            if (request.Role != Roles.Admin && request.Role != Roles.Organizer && request.Role != Roles.Attendee)
            {
                _logger.LogWarning("Invalid role specified for user {UserId}: {Role}", userId, request.Role);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.InvalidRole
                };
            }

            user.Role = request.Role;
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Role changed for user {UserId} to {Role}", userId, request.Role);

            if (request.Role == Roles.Organizer)
                _ = Task.Run(() => _notificationService.SendRoleChangedAsync(userId));

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.RoleChanged,
                Data = true
            };
        }
    }
}
