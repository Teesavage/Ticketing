using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using AutoMapper;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Ticketing.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ticketing.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ILogger<EventService> _logger;
        private readonly IEmailService _emailService;

        public UserService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config, ILogger<EventService> logger, IEmailService emailService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<ApiResponse<UserResponse>> RegisterUserAsync(UserRequest request)
        {
            if (request == null)
            {
                return ApiResponse<UserResponse>.FailureResponse(["Invalid User Data."]);
            }
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<UserResponse>.FailureResponse(["All fields are required."]);
            }
            if (!IsValidEmail(request.Email))
                return ApiResponse<UserResponse>.FailureResponse(["Invalid email format."]);
                
            var validationErrors = ValidatePassword(request.Password);
            if (validationErrors.Count > 0)
                return ApiResponse<UserResponse>.FailureResponse(validationErrors);

            // Check if user already exists
            var existingUser = await _unitOfWork.Users.Get(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return ApiResponse<UserResponse>.FailureResponse(["Email already in use."]);
            }

            var userEntity = _mapper.Map<User>(request);

            userEntity.Id = Guid.NewGuid();
            userEntity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            userEntity.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.Insert(userEntity);
            await _unitOfWork.Save();

            // Add user to UserRoles table
            var userRole = new UserRole
            {
                UserId = userEntity.Id,
                RoleId = request.RoleId 
            };

            await _unitOfWork.UserRoles.Insert(userRole);
            await _unitOfWork.Save();

            await _emailService.SendWelcomeEmail(userEntity.Email, userEntity.FirstName);

            var userResponse = _mapper.Map<UserResponse>(userEntity);

            return ApiResponse<UserResponse>.SuccessResponse(userResponse, "User registered successfully.");
        }

        public async Task<ApiResponse<UserResponse>> GetUserByIdAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId, includes: ["Role"]);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(["User not found."]);

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<UserResponse>> GetUserByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.Get(u => u.Email == email, includes: ["Role"]);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(["User not found."]);

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAll(includes: ["Role"]);
            var response = _mapper.Map<IEnumerable<UserResponse>>(users);
            return ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(response);
        }

        public async Task<ApiResponse<LoginResponse>> LoginUserAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<LoginResponse>.FailureResponse(["Email and password are required."]);

            var user = await _unitOfWork.Users.Get(u => u.Email == request.Email, includes: ["Role"]);
            if (user == null)
                return ApiResponse<LoginResponse>.FailureResponse(["Invalid email or password."]);

            // Verify password using BCrypt
            bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isValid)
                return ApiResponse<LoginResponse>.FailureResponse(["Invalid email or password."]);

            var token = GenerateJwt(user);
            var response = _mapper.Map<LoginResponse>(user);
            response.Token = token;

            _logger.LogInformation(
                "User {UserId} logged in at {Time}",
                user.Id,
                DateTime.UtcNow
            );
            return ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful.");
        }

        public async Task<ApiResponse<UserResponse>> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(["User not found."]);

            var errors = new List<string>();

            // Validation - only validate fields that are being updated (not null)
            if (request.FirstName != null && string.IsNullOrWhiteSpace(request.FirstName))
                errors.Add("First name cannot be empty");
            if (request.LastName != null && string.IsNullOrWhiteSpace(request.LastName))
                errors.Add("Last name cannot be empty");
            if (request.Email != null && string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email cannot be empty");
            if (request.PhoneNumber != null && string.IsNullOrWhiteSpace(request.PhoneNumber))
                errors.Add("Phone number cannot be empty");

            if (errors.Count == 0)
                    return ApiResponse<UserResponse>.FailureResponse(errors);

            // Update only non-null properties
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;
            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.Save();

            _logger.LogInformation(
                "User {UserId} updated their profile at {UpdateTime}",
                user.Id,
                DateTime.UtcNow
            );
            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response, "User updated successfully.");
        }

        public async Task<ApiResponse<UserResponse>> UpdateUserRoleAsync(Guid userId, UpdateUserRole request)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId, includes: ["Role"]);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(["User not found."]);

            user.RoleId = request.RoleId != Guid.Empty ? request.RoleId : user.RoleId;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.Save();
            _logger.LogInformation(
                "User {UserId} updated their role to {Role} at {UpdateTime}",
                user.Id,
                user.Role?.RoleName,
                DateTime.UtcNow
            );

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response, "User role updated successfully.");
        }

        public async Task<ApiResponse<string>> DeleteUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId);
            if (user == null)
                return ApiResponse<string>.FailureResponse(["User not found."]);

            await _unitOfWork.Users.DeleteGuid(user.Id);
            await _unitOfWork.Save();

            _logger.LogInformation(
                "User {UserId} was deleted at {UpdateTime}",
                userId,
                DateTime.UtcNow
            );

            return ApiResponse<string>.SuccessResponse("User deleted successfully.");
        }

        public async Task<ApiResponse<string>> ChangePassword(ChangePasswordRequest request)
        {
            var user = await _unitOfWork.Users.Get(u => u.Email == request.Email);
            if (user == null)
                return ApiResponse<string>.FailureResponse(["User not found."]);
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return ApiResponse<string>.FailureResponse(["Current password is incorrect."]);
            var validationErrors = ValidatePassword(request.NewPassword);
            if (validationErrors.Count > 0)
                return ApiResponse<string>.FailureResponse(validationErrors);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.Save();

            // await _gridEmailService.SendPasswordChangedEmail(request.Email);
            // _logger.LogInformation($"User {user.Id} changed password at {DateTime.UtcNow}");
            _logger.LogInformation(
                "User {UserId} changed password at {ResetTime}",
                user.Id,
                DateTime.UtcNow
            );
            return ApiResponse<string>.SuccessResponse("Password updated successfully.");
        }

        public async Task<ApiResponse<string>> ResetPassword(PasswordReset request)
        {
            // Validate password
            var validationErrors = ValidatePassword(request.NewPassword);
            if (validationErrors.Count > 0)
                return ApiResponse<string>.FailureResponse(validationErrors);

            var user = await _unitOfWork.Users.Get(u => u.Email == request.Email);
            if (user == null)
                return ApiResponse<string>.FailureResponse(new List<string> { "User not found." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.Save();
            
            // await _gridEmailService.SendPasswordChangedEmail(adminUser.Email);
            // _logger.LogInformation($"User {user.Id} reset password at {DateTime.UtcNow}");
            _logger.LogInformation(
                "User {UserId} reset password at {ResetTime}",
                user.Id,
                DateTime.UtcNow
            );

            return ApiResponse<string>.SuccessResponse("Successful", "Password reset successful.");
        }

        private string GenerateJwt(User user)
        {
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"], 
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private List<string> ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long.");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsSymbol) && !password.Any(char.IsPunctuation))
                errors.Add("Password must contain at least one special character (e.g., @, #, $, etc.).");

            return errors;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }





    }
}