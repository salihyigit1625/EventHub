namespace EventHub.Application.Common;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Attendee = "Attendee";
    public const string GateStaff = "GateStaff";

    public static readonly string[] Registrable = [Organizer, Attendee];
}
