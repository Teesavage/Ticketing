namespace Ticketing.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public required string RoleName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedBy { get; set; }

    }
}