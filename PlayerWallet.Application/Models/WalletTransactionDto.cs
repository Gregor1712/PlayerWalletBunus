using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Application.Models;

public class WalletTransactionDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid PlayerId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public bool IsAccepted { get; set; }
}