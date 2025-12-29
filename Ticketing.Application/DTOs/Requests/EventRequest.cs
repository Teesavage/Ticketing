using Ticketing.Domain;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.DTOs.Requests
{
    public class EventRequest
    {
        public required string EventTitle { get; set; }
        public required string EventDescription { get; set; }
        // public required string OrganizerEmail { get; set; }
        // public string? OrganizerPhoneNo { get; set; }
        public required string Location { get; set; }
        public DateTime? EventDateTime { get; set; }
        public EventType? EventType { get; set; } //online or physical
        // public required IList<TicketType> TicketTypes { get; set; }
        public required List<TicketTypeRequest> TicketTypes { get; set; }
        public required Guid CreatedBy { get; set; }
    }

    // For updating event (without tickets)
    public class EventUpdateRequest
    {
        public string? EventTitle { get; set; }
        public string? EventDescription { get; set; }
        public string? Location { get; set; }
        public DateTime? EventDateTime { get; set; }
        public EventType? EventType { get; set; }
    }

    // public class CreateTicketTypeDto
    // {
    //     public required string Type { get; set; }
    //     public required decimal Price { get; set; }
    //     public required int QuantityAvailable { get; set; }
    // }

    // For adding/updating ticket types
    public class TicketTypeRequest
    {
        public string? Type { get; set; }
        public decimal Price { get; set; }
        public int QuantityAvailable { get; set; }
    }

    public class TicketRequest
    {
        public Guid UserId { get; set; }
        public int TicketTypeId { get; set; }
        public DateTime PurchaseTime { get; set; }
        public int Quantity { get; set; }
    }

}


