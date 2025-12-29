using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketing.Domain.Entities
{
    public class Event
    {
        public long Id { get; set; }
        public required string EventTitle { get; set; }
        public required string EventDescription { get; set; }
        public required string OrganizerEmail { get; set; }
        public string? OrganizerPhoneNo { get; set; }
        public required string Location { get; set; }
        public required DateTime EventDateTime { get; set; }
        // public required string EventTime { get; set; }
        public EventType EventType { get; set; } //online or physical
        public required Guid CreatedBy { get; set; }
        [ForeignKey(nameof(CreatedBy))]
        public User? Creator { get; set; }
        public required IList<TicketType> TicketTypes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }


    }
}