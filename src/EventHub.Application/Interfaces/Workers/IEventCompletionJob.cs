namespace EventHub.Application.Interfaces.Workers;

public interface IEventCompletionJob
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
