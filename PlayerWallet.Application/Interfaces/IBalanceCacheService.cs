namespace PlayerWallet.Application.Interfaces;

public interface IBalanceCacheService
{
    Task<decimal?> GetBalanceAsync(Guid playerId);
    Task SetBalanceAsync(Guid playerId, decimal balance);
    Task InvalidateBalanceAsync(Guid playerId);
}