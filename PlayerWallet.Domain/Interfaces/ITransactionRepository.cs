using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<WalletTransaction?> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default);
    Task Add(WalletTransaction walletTransaction, CancellationToken cancellationToken = default);
    Task UpdateToAccepted(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTransaction>> GetTransactionByPlayerId(Guid playerId, CancellationToken cancellationToken = default);
}
