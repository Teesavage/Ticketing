using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using Ticketing.Infrastructure.PaginationHelper;

namespace Ticketing.Application
{
    public interface IUserService
    {
        public Task<ApiResponse<UserResponse>> RegisterUserAsync(UserRequest request);
        public Task<ApiResponse<UserResponse>> LoginUserAsync(LoginRequest request);
        public Task<ApiResponse<UserResponse>> GetUserByIdAsync(Guid userId);
        // public Task<ApiResponse<IList<UserResponse>>> GetAllUsersAsync(PaginationFilter paginationFilter);
        public Task<ApiResponse<UserResponse>> GetUserByEmailAsync(string email);
        public Task<ApiResponse<UserResponse>> UpdateUserAsync(Guid userId, UpdateUserRequest request);
        public Task<ApiResponse<string>> DeleteUserAsync(Guid userId);
    }

}

