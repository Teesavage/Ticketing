using Microsoft.AspNetCore.Mvc;
using Ticketing.Application;
using Ticketing.Application.DTOs.Requests;

namespace Ticketing.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("createRole")]
        public async Task<IActionResult> CreateRole([FromBody] RoleRequest request)
        {
            var response = await _roleService.CreateRoleAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getRoleById")]
        public async Task<IActionResult> GetRoleById(Guid roleId)
        {
            var response = await _roleService.GetRoleByIdAsync(roleId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getAllRoles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var response = await _roleService.GetAllRolesAsync();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("updateRole")]
        public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request)
        {
            var response = await _roleService.UpdateRoleAsync(roleId, request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }
        
        [HttpDelete("deleteRoleById")]
        public async Task<IActionResult> DeleteRole(Guid roleId)
        {
            var response = await _roleService.DeleteRoleAsync(roleId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }
        
    }
}