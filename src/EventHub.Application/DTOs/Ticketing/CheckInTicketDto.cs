namespace EventHub.Application.DTOs.Ticketing;

public class CheckInTicketDto
{
    public string UniqueCode { get; set; } = string.Empty;
    public string? DeviceLocation { get; set; }
}
