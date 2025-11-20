namespace Ticketing.Domain.Entities
{
    public class UserRole
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

    }
}