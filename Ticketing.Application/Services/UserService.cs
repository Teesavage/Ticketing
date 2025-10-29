using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using AutoMapper;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<UserResponse>> RegisterUserAsync(UserRequest request)
        {
            if (request == null)
            {
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "Invalid User Data." });
            }
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "All fields are required." });
            }

            // Check if user already exists
            var existingUser = await _unitOfWork.Users.Get(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "Email already in use." });
            }

            var userEntity = _mapper.Map<User>(request);

            userEntity.Id = Guid.NewGuid();
            userEntity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _unitOfWork.Users.Insert(userEntity);
            await _unitOfWork.Save();

            var userResponse = _mapper.Map<UserResponse>(userEntity);

            return ApiResponse<UserResponse>.SuccessResponse(userResponse, "User registered successfully.");
        }

        public async Task<ApiResponse<UserResponse>> GetUserByIdAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "User not found." });

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<UserResponse>> GetUserByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.Get(u => u.Email == email);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "User not found." });

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAll();
            var response = _mapper.Map<IEnumerable<UserResponse>>(users);
            return ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(response);
        }

        public async Task<ApiResponse<UserResponse>> LoginUserAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "Email and password are required." });

            var user = await _unitOfWork.Users.Get(u => u.Email == request.Email);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "Invalid email or password." });

            // 🔐 Verify password using BCrypt
            bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isValid)
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "Invalid email or password." });

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response, "Login successful.");
        }

        public async Task<ApiResponse<UserResponse>> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId);
            if (user == null)
                return ApiResponse<UserResponse>.FailureResponse(new List<string> { "User not found." });

            // Update properties
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.Email = request.Email ?? user.Email;
            user.RoleId = request.RoleId != Guid.Empty ? request.RoleId : user.RoleId;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.Save();

            var response = _mapper.Map<UserResponse>(user);
            return ApiResponse<UserResponse>.SuccessResponse(response, "User updated successfully.");
        }

        public async Task<ApiResponse<string>> DeleteUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.Get(u => u.Id == userId);
            if (user == null)
                return ApiResponse<string>.FailureResponse(new List<string> { "User not found." });

            await _unitOfWork.Users.DeleteGuid(user.Id);
            await _unitOfWork.Save();

            return ApiResponse<string>.SuccessResponse("User deleted successfully.");
        }





    }
}