using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Ticketing.Domain.Entities
{
    public class Ticket
    {
        public Guid TicketId { get; set; }
        public int TicketTypeId { get; set; }
        public TicketType? TicketType { get; set; } //event details from TicketType
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public DateTime PurchaseTime { get; set; }

    }

    public class TicketType
    {
        public int Id { get; set; }
        public string? Type { get; set; }  // e.g. EarlyBird, Regular, VIP, VVIP
        public decimal Price { get; set; }
        public int QuantityAvailable { get; set; }
        public long EventId { get; set; }
        
        [JsonIgnore]
        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }
        
    }
}