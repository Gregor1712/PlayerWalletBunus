using PlayerWallet.Application.Interfaces;

namespace PlayerWallet.Application.Services;

public class NoOpBalanceCacheService : IBalanceCacheService
{
    public Task<decimal?> GetBalanceAsync(Guid playerId) => Task.FromResult<decimal?>(null);
    public Task SetBalanceAsync(Guid playerId, decimal balance) => Task.CompletedTask;
    public Task InvalidateBalanceAsync(Guid playerId) => Task.CompletedTask;
}