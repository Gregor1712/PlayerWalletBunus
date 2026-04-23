using System.Globalization;
using Microsoft.Extensions.Logging;
using PlayerWallet.Application.Interfaces;
using StackExchange.Redis;

namespace PlayerWallet.Infrastructure.Redis;

public class RedisBalanceCacheService : IBalanceCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisBalanceCacheService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public RedisBalanceCacheService(IConnectionMultiplexer redis, ILogger<RedisBalanceCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private static string Key(Guid playerId) => $"wallet:balance:{playerId}";

    public async Task<decimal?> GetBalanceAsync(Guid playerId)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(Key(playerId));

        if (value.IsNullOrEmpty)
        {
            _logger.LogDebug("Cache miss for player {PlayerId}", playerId);
            return null;
        }

        _logger.LogDebug("Cache hit for player {PlayerId}", playerId);
        return decimal.Parse(value.ToString(), CultureInfo.InvariantCulture);
    }

    public async Task SetBalanceAsync(Guid playerId, decimal balance)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            Key(playerId),
            balance.ToString(CultureInfo.InvariantCulture),
            CacheTtl);

        _logger.LogDebug("Cache set for player {PlayerId}, Balance={Balance}", playerId, balance);
    }

    public async Task InvalidateBalanceAsync(Guid playerId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(playerId));
        _logger.LogDebug("Cache invalidated for player {PlayerId}", playerId);
    }
}