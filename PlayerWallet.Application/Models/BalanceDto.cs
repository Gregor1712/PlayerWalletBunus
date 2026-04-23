namespace PlayerWallet.Application.Models;

public class BalanceDto
{
    public Guid PlayerId { get; set; }
    public decimal Balance { get; set; }

    public BalanceDto(Guid playerId, decimal balance)
    {
        PlayerId = playerId;
        Balance = balance;
    }
}