namespace EventHub.Application.Workers;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public int StartupDelaySeconds { get; set; } = 10;
}
