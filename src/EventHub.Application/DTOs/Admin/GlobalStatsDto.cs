namespace EventHub.Application.DTOs.Admin;

public class GlobalStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public int PublishedEvents { get; set; }
    public int TicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingOrganizers { get; set; }
}
