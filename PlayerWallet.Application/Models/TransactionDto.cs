using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Application.Models;

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }

    public TransactionDto(Guid id, decimal amount, TransactionType type)
    {
        Id = id;
        Amount = amount;
        Type = type;
    }
}