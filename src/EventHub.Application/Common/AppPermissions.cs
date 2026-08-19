namespace EventHub.Application.Common;

public static class AppPermissions
{
    public const string EventsCreate = "events.create";
    public const string EventsUpdate = "events.update";
    public const string EventsPublish = "events.publish";
    public const string EventsCancel = "events.cancel";
    public const string EventsPosterUpload = "events.poster.upload";

    public const string TicketTypesManage = "tickettypes.manage";

    public const string TicketsPurchase = "tickets.purchase";
    public const string TicketsCancel = "tickets.cancel";
    public const string TicketsView = "tickets.view";
    public const string TicketsCheckIn = "tickets.checkin";

    public const string WaitlistJoin = "waitlist.join";
    public const string WaitlistConvert = "waitlist.convert";
    public const string WaitlistNotify = "waitlist.notify";

    public const string WalletView = "wallet.view";
    public const string WalletDeposit = "wallet.deposit";

    public const string PaymentsView = "payments.view";

    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsDownload = "documents.download";

    public const string OrganizersView = "organizers.view";
    public const string OrganizersUpdate = "organizers.update";

    public const string AdminApprove = "admin.approve";
    public const string AdminStats = "admin.stats";
    public const string AdminGateStaff = "admin.gatestaff";

    public static readonly string[] All =
    [
        EventsCreate,
        EventsUpdate,
        EventsPublish,
        EventsCancel,
        EventsPosterUpload,
        TicketTypesManage,
        TicketsPurchase,
        TicketsCancel,
        TicketsView,
        TicketsCheckIn,
        WaitlistJoin,
        WaitlistConvert,
        WaitlistNotify,
        WalletView,
        WalletDeposit,
        PaymentsView,
        DocumentsUpload,
        DocumentsDownload,
        OrganizersView,
        OrganizersUpdate,
        AdminApprove,
        AdminStats,
        AdminGateStaff
    ];
}
