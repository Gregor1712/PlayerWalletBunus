using AutoMapper;
using PlayerWallet.Application.Models;
using PlayerWallet.Domain.Interfaces;

namespace PlayerWallet.Application.Managers;

public class WalletManager : IWalletManager
{
    private readonly IWalletRepository _walletRepository;
    private readonly IMapper _mapper;

    public WalletManager(IWalletRepository walletRepository, IMapper mapper)
    {
        _walletRepository = walletRepository;
        _mapper = mapper;
    }

    public async Task<WalletDto?> Create(Guid playerId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.Create(playerId, cancellationToken);
        return wallet is null 
            ? null 
            : _mapper.Map<WalletDto>(wallet);
    }

    public async Task<WalletDto?> GetByPlayerId(Guid playerId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetWalletByPlayerId(playerId, cancellationToken);
        return wallet is null 
            ? null 
            : _mapper.Map<WalletDto>(wallet);
    }

    public Task UpdateBalance(Guid playerId, decimal newBalance, CancellationToken cancellationToken = default)
    {
        return _walletRepository.UpdateBalance(playerId, newBalance, cancellationToken);
    }
}