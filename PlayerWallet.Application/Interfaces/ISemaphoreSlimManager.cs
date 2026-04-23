namespace PlayerWallet.Application.Interfaces;

public interface ISemaphoreSlimManager
{
    Task<IAsyncDisposable> LockAsync(Guid playerId, CancellationToken cancellationToken = default);
    int Count { get; }
}