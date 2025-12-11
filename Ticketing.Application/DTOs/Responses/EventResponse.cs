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
        public required IList<TicketType> TicketTypes { get; set; }
    }


}


