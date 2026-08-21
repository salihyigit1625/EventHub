namespace EventHub.Application.DTOs.Events;

public class TicketTypeDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int MaxTicketsPerUser { get; set; }
    public DateTime SaleStartDate { get; set; }
    public DateTime SaleEndDate { get; set; }
}
