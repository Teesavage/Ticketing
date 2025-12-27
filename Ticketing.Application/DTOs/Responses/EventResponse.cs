using Ticketing.Domain;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.DTOs.Responses
{
    public class EventResponse
    {
        public long Id { get; set; }
        public required string EventTitle { get; set; }
        public required string EventDescription { get; set; }
        public required string OrganizerEmail { get; set; }
        public string? OrganizerPhoneNo { get; set; }
        public required string Location { get; set; }
        public required DateOnly EventDate { get; set; }
        public required string EventTime { get; set; }
        public EventType EventType { get; set; } //online or physical
        public string? EventTypeName { get; set; }
        public Guid CreatedBy { get; set; }
        public string? CreatorName { get; set; }
        public bool IsActive { get; set; }
        public required IList<TicketType> TicketTypes { get; set; }
    }

    // For ticket response
    public class TicketTypeResponse
    {
        public long Id { get; set; }
        public string? Type { get; set; }
        public decimal Price { get; set; }
        public int QuantityAvailable { get; set; }
        public long EventId { get; set; }
    }

    public class TicketResponse
    {
        public Guid TicketId { get; set; }
        public string? UserId { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        // public int TicketTypeId { get; set; }
        public string? TicketType { get; set; }
        public decimal TicketPrice { get; set; }
        public long? EventId { get; set; }
        public string? EventTitle { get; set; }
        public string? EventTime { get; set; }
        public DateTime PurchaseTime { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
    }


}


