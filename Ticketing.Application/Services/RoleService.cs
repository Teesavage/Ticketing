using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using AutoMapper;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;
using Microsoft.Extensions.Configuration;

namespace Ticketing.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public RoleService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
        }

        public async Task<ApiResponse<RoleResponse>> CreateRoleAsync(RoleRequest request)
        {
            if (request == null)
            {
                return ApiResponse<RoleResponse>.FailureResponse(new List<string> { "Invalid Role Data." });
            }
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                return ApiResponse<RoleResponse>.FailureResponse(new List<string> { "All fields are required." });
            }

            // Check if role already exists
            var existingRole = await _unitOfWork.Roles.Get(u => u.RoleName == request.RoleName);

            if (existingRole != null)
            {
                return ApiResponse<RoleResponse>.FailureResponse(new List<string> { "Role already exists." });
            }

            var roleEntity = _mapper.Map<Role>(request);

            roleEntity.Id = Guid.NewGuid();
            roleEntity.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Roles.Insert(roleEntity);
            await _unitOfWork.Save();

            var roleResponse = _mapper.Map<RoleResponse>(roleEntity);

            return ApiResponse<RoleResponse>.SuccessResponse(roleResponse, "Role created successfully.");
        }

        public async Task<ApiResponse<RoleResponse>> GetRoleByIdAsync(Guid roleId)
        {
            var role = await _unitOfWork.Roles.Get(u => u.Id == roleId);
            if (role == null)
                return ApiResponse<RoleResponse>.FailureResponse(new List<string> { "Role not found." });

            var response = _mapper.Map<RoleResponse>(role);
            return ApiResponse<RoleResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<RoleResponse>> GetRoleByNameAsync(string roleName)
        {
            var role = await _unitOfWork.Roles.Get(u => u.RoleName == roleName);
            if (role == null)
                return ApiResponse<RoleResponse>.FailureResponse(new List<string> { "Role not found." });

            var response = _mapper.Map<RoleResponse>(role);
            return ApiResponse<RoleResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<IEnumerable<RoleResponse>>> GetAllRolesAsync()
        {
            var roles = await _unitOfWork.Roles.GetAll();
            var response = _mapper.Map<IEnumerable<RoleResponse>>(roles);
            return ApiResponse<IEnumerable<RoleResponse>>.SuccessResponse(response);
        }

        public async Task<ApiResponse<RoleResponse>> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
        {
            var role = await _unitOfWork.Roles.Get(u => u.Id == roleId);
            if (role == null)
                return ApiResponse<RoleResponse>.FailureResponse(new List<string> { "Role not found." });

            // Update properties
            role.RoleName = request.RoleName ?? role.RoleName;

            _unitOfWork.Roles.Update(role);
            await _unitOfWork.Save();

            var response = _mapper.Map<RoleResponse>(role);
            return ApiResponse<RoleResponse>.SuccessResponse(response, "Role updated successfully.");
        }

        public async Task<ApiResponse<string>> DeleteRoleAsync(Guid roleId)
        {
            var role = await _unitOfWork.Roles.Get(u => u.Id == roleId);
            if (role == null)
                return ApiResponse<string>.FailureResponse(new List<string> { "Role not found." });

            await _unitOfWork.Roles.DeleteGuid(role.Id);
            await _unitOfWork.Save();

            return ApiResponse<string>.SuccessResponse("Role deleted successfully.");
        }

    }
}