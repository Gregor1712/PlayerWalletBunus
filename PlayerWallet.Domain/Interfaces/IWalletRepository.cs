using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Domain.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> Create(Guid playerId, CancellationToken cancellationToken = default);
    Task<Wallet?> GetWalletByPlayerId(Guid playerId, CancellationToken cancellationToken = default);
    Task UpdateBalance(Guid playerId, decimal newBalance, CancellationToken cancellationToken = default);
}
