namespace EventHub.Application.DTOs.Events;

public class CreateTicketTypeDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int MaxTicketsPerUser { get; set; } = 5;
    public DateTime SaleStartDate { get; set; }
    public DateTime SaleEndDate { get; set; }
}
