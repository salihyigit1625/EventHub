namespace EventHub.Application.DTOs.Events;

public class UpdateEventDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Venue { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CancellationDeadlineHours { get; set; }
}
