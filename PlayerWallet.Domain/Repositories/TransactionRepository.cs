using PlayerWallet.Domain.Entities;
using PlayerWallet.Domain.Interfaces;

namespace PlayerWallet.Domain.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly List<WalletTransaction> _transactions = new();

    public async Task<WalletTransaction?> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return _transactions.FirstOrDefault(t => t.TransactionId == transactionId);
    }

    public async Task Add(WalletTransaction walletTransaction, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        _transactions.Add(walletTransaction);
    }

    public async Task UpdateToAccepted(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        var transaction = _transactions.FirstOrDefault(t => t.Id == id);
        if (transaction is not null) transaction.IsAccepted = true;
    }

    public Task<IReadOnlyList<WalletTransaction>> GetTransactionByPlayerId(Guid playerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WalletTransaction> result = _transactions
            .Where(t => t.PlayerId == playerId)
            .ToList();
        return Task.FromResult(result);
    }
}
