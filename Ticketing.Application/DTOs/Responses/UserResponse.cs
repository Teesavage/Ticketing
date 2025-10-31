namespace Ticketing.Application.DTOs.Responses
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public required string Password { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
    public class LoginResponse : UserResponse
    {
        public string? Token { get; set; }
    }

}


