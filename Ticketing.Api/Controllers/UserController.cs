using Microsoft.AspNetCore.Mvc;
using Ticketing.Application;
using Ticketing.Application.DTOs.Requests;

namespace Ticketing.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("registerClient")]
        public async Task<IActionResult> RegisterClient([FromBody] UserRequest request)
        {
            // request.RoleId = new Guid("f2a2f1b4-6b8b-4cb9-8e8e-55d999d3b8cd"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("registerOrganizer")]
        public async Task<IActionResult> RegisterOrganizer([FromBody] UserRequest request)
        {
            // request.RoleId = new Guid("f2a2f1b4-6b8b-4cb9-8e8e-55d999d3b8cd"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("registerAdmin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] UserRequest request)
        {
            // request.RoleId = new Guid("f2a2f1b4-6b8b-4cb9-8e8e-55d999d3b8cd"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("registerManager")]
        public async Task<IActionResult> RegisterManager([FromBody] UserRequest request)
        {
            // request.RoleId = new Guid("f2a2f1b4-6b8b-4cb9-8e8e-55d999d3b8cd"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _userService.LoginUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getUserById/{userId}")]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var response = await _userService.GetUserByIdAsync(userId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getUserByEmail/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var response = await _userService.GetUserByEmailAsync(email);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = await _userService.GetAllUsersAsync();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("updateUser/{userId}")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
        {
            var response = await _userService.UpdateUserAsync(userId, request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("updateUserRole/{userId}")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRole request)
        {
            var response = await _userService.UpdateUserRoleAsync(userId, request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("deleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var response = await _userService.DeleteUserAsync(userId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

    }

};

