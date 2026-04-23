namespace PlayerWallet.Application.Exceptions;

public class WalletAlreadyExistsException(Guid playerId)
    : Exception($"Wallet already registered for player '{playerId}'.");
