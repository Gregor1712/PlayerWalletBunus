namespace PlayerWallet.Domain.Entities;

public class Wallet
{
    public Guid PlayerId { get; set; }
    public decimal Balance { get; set; }
}
