using Microsoft.Extensions.Logging;
using PlayerWallet.Application.Interfaces;
using RedLockNet;
using RedLockNet.SERedis;

namespace PlayerWallet.Infrastructure.Redis;

public class RedisDistributedLockManager : ISemaphoreSlimManager
{
    private readonly RedLockFactory _redLockFactory;
    private readonly ILogger<RedisDistributedLockManager> _logger;

    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockRetry = TimeSpan.FromMilliseconds(200);

    public RedisDistributedLockManager(RedLockFactory redLockFactory, ILogger<RedisDistributedLockManager> logger)
    {
        _redLockFactory = redLockFactory;
        _logger = logger;
    }

    // Not meaningful for distributed locks, but satisfies the interface.
    public int Count => 0;

    public async Task<IAsyncDisposable> LockAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var resource = $"wallet:lock:{playerId}";

        var redLock = await _redLockFactory.CreateLockAsync(
            resource, LockExpiry, LockWait, LockRetry, cancellationToken);

        if (!redLock.IsAcquired)
        {
            _logger.LogWarning("Failed to acquire distributed lock for player {PlayerId}", playerId);
            throw new TimeoutException($"Could not acquire distributed lock for player {playerId}");
        }

        _logger.LogDebug("Distributed lock acquired for player {PlayerId}", playerId);
        return new RedLockReleaser(redLock, _logger, playerId);
    }

    private class RedLockReleaser : IAsyncDisposable
    {
        private readonly IRedLock _redLock;
        private readonly ILogger _logger;
        private readonly Guid _playerId;

        public RedLockReleaser(IRedLock redLock, ILogger logger, Guid playerId)
        {
            _redLock = redLock;
            _logger = logger;
            _playerId = playerId;
        }

        public ValueTask DisposeAsync()
        {
            _redLock.Dispose();
            _logger.LogDebug("Distributed lock released for player {PlayerId}", _playerId);
            return ValueTask.CompletedTask;
        }
    }
}