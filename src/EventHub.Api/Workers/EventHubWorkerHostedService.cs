using EventHub.Application.Interfaces.Workers;
using EventHub.Application.Workers;
using Microsoft.Extensions.Options;

namespace EventHub.Api.Workers;

public sealed class EventHubWorkerHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerOptions> options,
    ILogger<EventHubWorkerHostedService> logger) : BackgroundService
{
    private readonly SemaphoreSlim _runLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = Math.Max(options.Value.StartupDelaySeconds, 0);
        if (startupDelay > 0)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(startupDelay), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        var intervalSeconds = Math.Max(options.Value.IntervalSeconds, 1);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        logger.LogInformation(
            "EventHub worker started. Enabled={Enabled}, IntervalSeconds={IntervalSeconds}.",
            options.Value.Enabled,
            intervalSeconds);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!options.Value.Enabled)
                continue;

            if (!await _runLock.WaitAsync(0, stoppingToken))
            {
                logger.LogDebug("EventHub worker skipped tick; previous run still in progress.");
                continue;
            }

            try
            {
                await RunJobsAsync(stoppingToken);
            }
            finally
            {
                _runLock.Release();
            }
        }
    }

    private async Task RunJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var completionJob = scope.ServiceProvider.GetRequiredService<IEventCompletionJob>();
        var holdExpiryJob = scope.ServiceProvider.GetRequiredService<IWaitlistHoldExpiryJob>();

        try
        {
            await completionJob.RunAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "EventCompletionJob failed.");
        }

        try
        {
            await holdExpiryJob.RunAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "WaitlistHoldExpiryJob failed.");
        }
    }

    public override void Dispose()
    {
        _runLock.Dispose();
        base.Dispose();
    }
}
