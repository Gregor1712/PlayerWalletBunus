namespace PlayerWallet.Application.Models;

public class RegisterWalletDto
{
    public Guid PlayerId { get; set; }
    public decimal Balance { get; set; }

    public RegisterWalletDto(Guid playerId, decimal balance)
    {
        PlayerId = playerId;
        Balance = balance;
    }
}