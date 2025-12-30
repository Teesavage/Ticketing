namespace Ticketing.Domain.Entities
{
    public class Country
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? ISO { get; set; }

    }
}