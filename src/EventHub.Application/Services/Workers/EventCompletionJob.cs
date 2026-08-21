using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Workers;
using EventHub.Application.Workers;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Application.Services.Workers;

public class EventCompletionJob(
    IGenericRepository<Event> eventRepository,
    IUnitOfWork unitOfWork,
    IOptions<WorkerOptions> options,
    ILogger<EventCompletionJob> logger) : IEventCompletionJob
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var batchSize = Math.Max(options.Value.BatchSize, 1);

        var due = await eventRepository.FindAsync(
            e => e.Status == EventStatus.Published && e.EndDate < now,
            cancellationToken);

        var batch = due.Take(batchSize).ToList();
        if (batch.Count == 0)
            return 0;

        foreach (var entity in batch)
        {
            entity.Status = EventStatus.Completed;
            eventRepository.Update(entity);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("EventCompletionJob marked {Count} event(s) as Completed.", batch.Count);
        return batch.Count;
    }
}
