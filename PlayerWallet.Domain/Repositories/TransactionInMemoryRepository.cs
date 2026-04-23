using Microsoft.EntityFrameworkCore;
using PlayerWallet.Domain.Entities;
using PlayerWallet.Domain.Interfaces;
using PlayerWallet.Domain.Data;

namespace PlayerWallet.Domain.Repositories;

public class TransactionInMemoryRepository : ITransactionRepository
{
    private readonly WalletDbContext _context;

    public TransactionInMemoryRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<WalletTransaction?> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == transactionId, cancellationToken);
    }

    public async Task Add(WalletTransaction walletTransaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(walletTransaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateToAccepted(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions.FindAsync([id], cancellationToken);
        if (transaction is not null)
        {
            transaction.IsAccepted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<WalletTransaction>> GetTransactionByPlayerId(Guid playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.PlayerId == playerId)
            .ToListAsync(cancellationToken);
    }
}
