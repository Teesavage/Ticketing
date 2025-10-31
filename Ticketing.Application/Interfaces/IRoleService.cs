using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using Ticketing.Infrastructure.PaginationHelper;

namespace Ticketing.Application
{
    public interface IRoleService
    {
        public Task<ApiResponse<RoleResponse>> CreateRoleAsync(RoleRequest request);
        public Task<ApiResponse<RoleResponse>> GetRoleByIdAsync(Guid roleId);
        public Task<ApiResponse<IEnumerable<RoleResponse>>> GetAllRolesAsync();
        public Task<ApiResponse<RoleResponse>> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);
        public Task<ApiResponse<string>> DeleteRoleAsync(Guid roleId);
    }


}

