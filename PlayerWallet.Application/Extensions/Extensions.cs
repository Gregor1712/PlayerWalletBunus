using PlayerWallet.Application.Models;
using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Application.Extensions;

public static class Extensions
{
    public static decimal CalculateNewBalance(this TransactionRequestDto requestDto, WalletDto wallet)
    {
        return requestDto.Type switch
        {
            TransactionType.Deposit or TransactionType.Win => wallet.Balance + requestDto.Amount,
            TransactionType.Stake => wallet.Balance - requestDto.Amount,
            _ => throw new ArgumentOutOfRangeException(nameof(requestDto.Type))
        };
    }

    public static WalletTransactionDto ToWalletTransactionDto(this TransactionRequestDto requestDto, Guid playerId, bool isAccepted)
    {
        return new WalletTransactionDto
        {
            Id = Guid.NewGuid(),
            TransactionId = requestDto.TransactionId,
            PlayerId = playerId,
            Type = requestDto.Type,
            Amount = requestDto.Amount,
            IsAccepted = isAccepted
        };
    }
}