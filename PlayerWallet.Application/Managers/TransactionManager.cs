using AutoMapper;
using PlayerWallet.Application.Models;
using PlayerWallet.Domain.Entities;
using PlayerWallet.Domain.Interfaces;

namespace PlayerWallet.Application.Managers;

public class TransactionManager : ITransactionManager
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;

    public TransactionManager(ITransactionRepository transactionRepository, IMapper mapper)
    {
        _transactionRepository = transactionRepository;
        _mapper = mapper;
    }

    public async Task<WalletTransactionDto?> GetByTransactionId(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByTransactionId(transactionId, cancellationToken);
        return transaction is null
            ? null
            : _mapper.Map<WalletTransactionDto>(transaction);
    }

    public Task Add(WalletTransactionDto transaction, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<WalletTransaction>(transaction);
        return _transactionRepository.Add(entity, cancellationToken);
    }

    public Task UpdateToAccepted(Guid id, CancellationToken cancellationToken = default)
    {
        return _transactionRepository.UpdateToAccepted(id, cancellationToken);
    }

    public async Task<IReadOnlyList<WalletTransactionDto>> GetByPlayerId(Guid playerId, CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetTransactionByPlayerId(playerId, cancellationToken);
        return _mapper.Map<IReadOnlyList<WalletTransactionDto>>(transactions);
    }
}