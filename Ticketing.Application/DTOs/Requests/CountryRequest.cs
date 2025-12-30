namespace Ticketing.Application.DTOs.Requests
{
    public class CountryRequest
    {
        public required string Name { get; set; }
        public required string ISO { get; set; }
    }

    public class StateRequest
    {
        public int CountryId { get; set; }
        public required string Name { get; set; }
    }
}


