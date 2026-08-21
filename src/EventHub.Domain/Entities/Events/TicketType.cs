using EventHub.Domain.Entities.Ticketing;

namespace EventHub.Domain.Entities.Events;

public class TicketType
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public virtual Event Event { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int MaxTicketsPerUser { get; set; } = 5;
    public DateTime SaleStartDate { get; set; }
    public DateTime SaleEndDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<Waitlist> Waitlists { get; set; } = new List<Waitlist>();
}