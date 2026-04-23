using Microsoft.AspNetCore.Mvc;
using PlayerWallet.Application.Interfaces;
using PlayerWallet.Application.Models;
using System.Net.Mime;

namespace PlayerWallet.Api.Controllers;

[ApiController]
[Route("api/players/{playerId:guid}")]
[Produces(MediaTypeNames.Application.Json)]
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpPost("wallet")]
    public async Task<ActionResult<RegisterWalletDto>> RegisterWallet(Guid playerId, CancellationToken cancellationToken)
    {
        var wallet = await walletService.RegisterWallet(playerId, cancellationToken);

        return CreatedAtAction(
            nameof(GetBalance),
            new { playerId = wallet.PlayerId },
            new RegisterWalletDto(wallet.PlayerId, wallet.Balance));
    }

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance(Guid playerId, CancellationToken cancellationToken)
    {
        var balance = await walletService.GetBalance(playerId, cancellationToken);
        return Ok(new BalanceDto(playerId, balance));
    }

    [HttpPost("transactions")]
    public async Task<ActionResult<TransactionResponseDto>> CreditTransaction(Guid playerId, 
        [FromBody] TransactionRequestDto requestDto, 
        CancellationToken cancellationToken)
    {
        var result = await walletService.CreditTransaction(playerId, requestDto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(Guid playerId, CancellationToken cancellationToken)
    {
        var transactions = await walletService.GetTransactions(playerId, cancellationToken);
        return Ok(transactions);
    }
}
