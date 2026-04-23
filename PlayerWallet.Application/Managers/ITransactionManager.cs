using PlayerWallet.Application.Models;

namespace PlayerWallet.Application.Managers;

public interface ITransactionManager
{
    Task<WalletTransactionDto?> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default);
    Task Add(WalletTransactionDto transaction, CancellationToken cancellationToken = default);
    Task UpdateToAccepted(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTransactionDto>> GetByPlayerId(Guid playerId, CancellationToken cancellationToken = default);
}