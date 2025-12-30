namespace Ticketing.Application.DTOs.Responses
{
    public class CountryResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ISO { get; set; }
    }

    public class StateResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CountryId { get; set; }
        public string? Country { get; set; }
    }
}


