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
            request.RoleId = new Guid("779dff9b-da5d-41cf-84ce-673b9aa0565f"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("registerOrganizer")]
        public async Task<IActionResult> RegisterOrganizer([FromBody] UserRequest request)
        {
            request.RoleId = new Guid("d0e966d9-5e07-4b0c-ac02-67895c1469c0"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("registerAdmin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] UserRequest request)
        {
            request.RoleId = new Guid("56cafdee-72aa-4dcd-a61b-6403f8a1063d"); //assign default roles
            var response = await _userService.RegisterUserAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("registerManager")]
        public async Task<IActionResult> RegisterManager([FromBody] UserRequest request)
        {
            request.RoleId = new Guid("2f16a56d-da86-4b89-a33e-956043a810ad"); //assign default roles
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

