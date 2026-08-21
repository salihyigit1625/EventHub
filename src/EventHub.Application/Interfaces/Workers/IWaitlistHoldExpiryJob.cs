namespace EventHub.Application.Interfaces.Workers;

public interface IWaitlistHoldExpiryJob
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
