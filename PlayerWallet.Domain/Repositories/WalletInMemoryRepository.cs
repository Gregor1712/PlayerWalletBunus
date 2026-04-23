using Microsoft.EntityFrameworkCore;
using PlayerWallet.Domain.Entities;
using PlayerWallet.Domain.Interfaces;
using PlayerWallet.Domain.Data;

namespace PlayerWallet.Domain.Repositories;

public class WalletInMemoryRepository : IWalletRepository
{
    private readonly WalletDbContext _context;

    public WalletInMemoryRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> Create(Guid playerId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Wallets.AnyAsync(w => w.PlayerId == playerId, cancellationToken);
        if (exists) return null;

        var wallet = new Wallet
        {
            PlayerId = playerId,
            Balance = 0m
        };

        _context.Wallets.Add(wallet);

        await _context.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    public async Task<Wallet?> GetWalletByPlayerId(Guid playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets.FindAsync([playerId], cancellationToken);
    }

    public async Task UpdateBalance(Guid playerId, decimal newBalance, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallets.FindAsync([playerId], cancellationToken);
        if (wallet is not null)
        {
            wallet.Balance = newBalance;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
