namespace EventHub.Domain.Enums;

public enum EventStatus
{
    Draft = 1,
    Published = 2,
    Cancelled = 3,
    Completed = 4
}

public enum TicketStatus
{
    Reserved = 1,
    Paid = 2,
    CheckedIn = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum WaitlistStatus
{
    Waiting = 1,
    Notified = 2,
    Expired = 3,
    Converted = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public enum WalletTransactionType
{
    Purchase = 1,
    Refund = 2,
    Deposit = 3
}