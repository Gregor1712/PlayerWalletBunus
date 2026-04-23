namespace PlayerWallet.Application.Exceptions;

public class WalletNotFoundException(Guid playerId)
    : Exception($"Player wallet not found for player '{playerId}'.");
