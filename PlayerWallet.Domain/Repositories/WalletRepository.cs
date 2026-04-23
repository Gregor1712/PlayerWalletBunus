using System.Collections.Concurrent;
using PlayerWallet.Domain.Entities;
using PlayerWallet.Domain.Interfaces;

namespace PlayerWallet.Domain.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ConcurrentDictionary<Guid, Wallet> _wallets = new();

    public Task<Wallet?> Create(Guid playerId, CancellationToken cancellationToken = default)
    {
        var wallet = new Wallet
        {
            PlayerId = playerId,
            Balance = 0m
        };
        return Task.FromResult(_wallets.TryAdd(playerId, wallet) ? wallet : null);
    }

    public async Task<Wallet?> GetWalletByPlayerId(Guid playerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return _wallets.GetValueOrDefault(playerId);
    }

    public async Task UpdateBalance(Guid playerId, decimal newBalance, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        if (_wallets.TryGetValue(playerId, out var wallet))
        {
            wallet.Balance = newBalance;
        }
    }
}
