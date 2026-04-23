namespace PlayerWallet.Application.Models;

public class TransactionResponseDto
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;

    public TransactionResponseDto(Guid transactionId, string status)
    {
        TransactionId = transactionId;
        Status = status;
    }
}