using PlayerWallet.Application.Models;

namespace PlayerWallet.Application.Interfaces;

public interface IWalletService
{
    Task<WalletDto> RegisterWallet(Guid playerId, CancellationToken cancellationToken = default);
    Task<decimal> GetBalance(Guid playerId, CancellationToken cancellationToken = default);
    Task<TransactionResponseDto> CreditTransaction(Guid playerId, TransactionRequestDto requestDto, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionDto>> GetTransactions(Guid playerId, CancellationToken cancellationToken = default);
}