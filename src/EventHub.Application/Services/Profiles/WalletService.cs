using AutoMapper;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Application.Interfaces.Persistence;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Storage;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;
using EventHub.Domain.Enums;

namespace EventHub.Application.Services.Profiles;

public class WalletService(
    IGenericRepository<AttendeeProfile> attendeeRepository,
    IGenericRepository<WalletTransaction> walletTransactionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMapper mapper) : IWalletService
{
    public async Task<WalletBalanceDto> DepositAsync(
        DepositDto dto,
        CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var attendee = await attendeeRepository.GetByIdAsync(attendeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"AttendeeProfile ({attendeeId}) was not found.");

        attendee.WalletBalance += dto.Amount;
        attendee.UpdatedAt = DateTime.UtcNow;

        await walletTransactionRepository.AddAsync(new WalletTransaction
        {
            AttendeeId = attendeeId,
            Amount = dto.Amount,
            BalanceAfter = attendee.WalletBalance,
            Type = WalletTransactionType.Deposit
        }, cancellationToken);

        attendeeRepository.Update(attendee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WalletBalanceDto { AttendeeId = attendeeId, Balance = attendee.WalletBalance };
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var attendee = await attendeeRepository.GetByIdAsync(attendeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"AttendeeProfile ({attendeeId}) was not found.");

        return new WalletBalanceDto { AttendeeId = attendeeId, Balance = attendee.WalletBalance };
    }

    public async Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(
        CancellationToken cancellationToken = default)
    {
        var attendeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var transactions = await walletTransactionRepository.FindAsync(
            t => t.AttendeeId == attendeeId,
            cancellationToken);

        return transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => mapper.Map<WalletTransactionDto>(t))
            .ToList();
    }
}
