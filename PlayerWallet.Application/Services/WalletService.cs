using PlayerWallet.Application.Exceptions;
using PlayerWallet.Application.Extensions;
using PlayerWallet.Application.Interfaces;
using PlayerWallet.Application.Managers;
using PlayerWallet.Application.Models;

namespace PlayerWallet.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletManager _walletManager;
    private readonly ITransactionManager _transactionManager;
    private readonly ISemaphoreSlimManager _semaphoreSlimManager;
    private readonly IBalanceCacheService _balanceCacheService;

    public WalletService(IWalletManager walletManager, ITransactionManager transactionManager, ISemaphoreSlimManager semaphoreSlimManager, IBalanceCacheService balanceCacheService)
    {
        _walletManager = walletManager;
        _transactionManager = transactionManager;
        _semaphoreSlimManager = semaphoreSlimManager;
        _balanceCacheService = balanceCacheService;
    }

    public async Task<WalletDto> RegisterWallet(Guid playerId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletManager.Create(playerId, cancellationToken);
        return wallet ?? throw new WalletAlreadyExistsException(playerId);
    }

    public async Task<decimal> GetBalance(Guid playerId, CancellationToken cancellationToken = default)
    {
        var cached = await _balanceCacheService.GetBalanceAsync(playerId);
        if (cached.HasValue) return cached.Value;

        var wallet = await GetWallet(playerId, cancellationToken);
        if (wallet is null) throw new WalletNotFoundException(playerId);

        await _balanceCacheService.SetBalanceAsync(playerId, wallet.Balance);
        return wallet.Balance;
    }

    private async Task<WalletDto?> GetWallet(Guid playerId, CancellationToken cancellationToken)
    {
        var wallet = await _walletManager.GetByPlayerId(playerId, cancellationToken);
        return wallet;
    }

    public async Task<TransactionResponseDto> CreditTransaction(Guid playerId, TransactionRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        // per-player async lock (waits if another request for this player is in progress).
        await using (await _semaphoreSlimManager.LockAsync(playerId, cancellationToken))
        {
            var wallet = await GetWallet(playerId, cancellationToken);
            if (wallet is null) throw new WalletNotFoundException(playerId);
            
            if (requestDto.TransactionId != Guid.Empty)
            {
                var existing = await _transactionManager.GetByTransactionId(requestDto.TransactionId, cancellationToken);
                if (existing is not null)
                {
                    // return if accepted
                    if (existing.IsAccepted)
                    {
                        return new TransactionResponseDto(existing.TransactionId, "accepted");
                    }

                    // previous rejected — re-evaluate with current balance
                    var retryBalance = requestDto.CalculateNewBalance(wallet);
                    if (retryBalance < 0)
                    {
                        return new TransactionResponseDto(existing.TransactionId, "rejected");  
                    }

                    await _transactionManager.UpdateToAccepted(existing.Id, cancellationToken);
                    await _walletManager.UpdateBalance(playerId, retryBalance, cancellationToken);
                    await _balanceCacheService.SetBalanceAsync(playerId, retryBalance);
                    return new TransactionResponseDto(existing.TransactionId, "accepted");
                }
            }

            var newBalance = requestDto.CalculateNewBalance(wallet);
            var isAccepted = newBalance >= 0;

            var transaction = requestDto.ToWalletTransactionDto(playerId, isAccepted);
            await _transactionManager.Add(transaction, cancellationToken);

            if (isAccepted)
            {
                await _walletManager.UpdateBalance(playerId, newBalance, cancellationToken);
                await _balanceCacheService.SetBalanceAsync(playerId, newBalance);
            }

            return new TransactionResponseDto(requestDto.TransactionId, isAccepted ? "accepted" : "rejected");
        }
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactions(Guid playerId, CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionManager.GetByPlayerId(playerId, cancellationToken);
        return transactions.Select(t => new TransactionDto(t.Id, t.Amount, t.Type));
    }
}