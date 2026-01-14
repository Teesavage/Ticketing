namespace Ticketing.Application.DTOs.Requests
{
    public class UserRequest
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public required string Password { get; set; }
        public Guid RoleId { get; set; }
    }

    public class UpdateUserRequest
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateUserRole
    {
        public Guid RoleId { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string Email { get; set; }
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
    public class PasswordReset
    {
        public required string Email { get; set; }
        public required string NewPassword { get; set; }
    }
}


