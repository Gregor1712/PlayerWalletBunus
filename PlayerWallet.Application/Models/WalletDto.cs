namespace PlayerWallet.Application.Models;

public class WalletDto
{
    public Guid PlayerId { get; set; }
    public decimal Balance { get; set; }
}