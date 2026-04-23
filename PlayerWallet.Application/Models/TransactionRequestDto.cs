using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Application.Models;

public class TransactionRequestDto
{
    public Guid TransactionId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
}