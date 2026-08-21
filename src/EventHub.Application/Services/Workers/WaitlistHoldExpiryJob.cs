using EventHub.Application.Common;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Workers;
using EventHub.Application.Workers;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Application.Services.Workers;

public class WaitlistHoldExpiryJob(
    IGenericRepository<Waitlist> waitlistRepository,
    IGenericRepository<TicketType> ticketTypeRepository,
    IUnitOfWork unitOfWork,
    IOptions<WorkerOptions> options,
    ILogger<WaitlistHoldExpiryJob> logger) : IWaitlistHoldExpiryJob
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var batchSize = Math.Max(options.Value.BatchSize, 1);

        var due = await waitlistRepository.FindAsync(
            w => w.Status == WaitlistStatus.Notified
                 && w.ExpiresAt != null
                 && w.ExpiresAt < now,
            cancellationToken);

        var batch = due.Take(batchSize).ToList();
        if (batch.Count == 0)
            return 0;

        var ticketTypeIds = batch.Select(w => w.TicketTypeId).Distinct().ToList();
        var ticketTypes = await ticketTypeRepository.FindAsync(
            t => ticketTypeIds.Contains(t.Id),
            cancellationToken);
        var ticketTypesById = ticketTypes.ToDictionary(t => t.Id);

        var expiredCount = 0;
        foreach (var entry in batch)
        {
            ticketTypesById.TryGetValue(entry.TicketTypeId, out var ticketType);
            if (!WaitlistHoldGuard.TryExpireHold(entry, ticketType, now))
                continue;

            waitlistRepository.Update(entry);
            if (ticketType is not null)
                ticketTypeRepository.Update(ticketType);
            expiredCount++;
        }

        if (expiredCount == 0)
            return 0;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "WaitlistHoldExpiryJob expired {Count} hold(s) and released reserved stock.",
            expiredCount);
        return expiredCount;
    }
}
