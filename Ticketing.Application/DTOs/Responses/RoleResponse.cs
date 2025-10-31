namespace Ticketing.Application.DTOs.Responses
{
    public class RoleResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
    }


}


