using PlayerWallet.Application.Models;

namespace PlayerWallet.Application.Managers;

public interface IWalletManager
{
    Task<WalletDto?> Create(Guid playerId, CancellationToken cancellationToken = default);
    Task<WalletDto?> GetByPlayerId(Guid playerId, CancellationToken cancellationToken = default);
    Task UpdateBalance(Guid playerId, decimal newBalance, CancellationToken cancellationToken = default);
}