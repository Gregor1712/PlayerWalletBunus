using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using PlayerWallet.Application.Exceptions;
using PlayerWallet.Application.Interfaces;
using PlayerWallet.Application.Managers;
using PlayerWallet.Application.Models;
using PlayerWallet.Domain.Entities;
using PlayerWallet.Domain.Data;
using PlayerWallet.Application.Services;
using PlayerWallet.Domain.Repositories;

namespace PlayerWallet.Tests;

public class WalletServiceInMemoryDatabaseTests : IDisposable
{
    private static readonly ILoggerFactory SharedLoggerFactory = LoggerFactory.Create(builder => builder.AddNLog(TestLogConfig.Create()).SetMinimumLevel(LogLevel.Debug));
    private readonly WalletDbContext _context;
    private readonly IWalletService _service;

    public WalletServiceInMemoryDatabaseTests()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new WalletDbContext(options);
        var mapper = TestMapperFactory.Create();
        var walletManager = new WalletManager(new WalletInMemoryRepository(_context), mapper);
        var transactionManager = new TransactionManager(new TransactionInMemoryRepository(_context), mapper);
        var lockManager = new SemaphoreSlimManager(SharedLoggerFactory.CreateLogger<SemaphoreSlimManager>());
        _service = new WalletService(walletManager, transactionManager, lockManager, new NoOpBalanceCacheService());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterWallet_ShouldCreateWalletWithZeroBalance()
    {
        var playerId = Guid.NewGuid();
        var wallet = await _service.RegisterWallet(playerId);

        Assert.NotNull(wallet);
        Assert.Equal(playerId, wallet.PlayerId);
        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public async Task RegisterWallet_DuplicatePlayer_ShouldThrow()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        await Assert.ThrowsAsync<WalletAlreadyExistsException>(
            () => _service.RegisterWallet(playerId));
    }

    [Fact]
    public async Task GetBalance_NonExistentPlayer_ShouldThrow()
    {
        await Assert.ThrowsAsync<WalletNotFoundException>(
            () => _service.GetBalance(Guid.NewGuid()));
    }

    [Fact]
    public async Task Deposit_ShouldIncrementBalance()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        var result = await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Deposit,
                Amount = 100m
            });

        Assert.Equal("accepted", result.Status);
        Assert.Equal(100m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task Win_ShouldIncrementBalance()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Deposit,
                Amount = 50m
            });

        var result = await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Win,
                Amount = 30m
            });

        Assert.Equal("accepted", result.Status);
        Assert.Equal(80m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task Stake_ShouldDecrementBalance()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Deposit,
                Amount = 100m
            });

        var result = await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Stake,
                Amount = 40m
            });

        Assert.Equal("accepted", result.Status);
        Assert.Equal(60m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task Stake_InsufficientFunds_ShouldReject()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Deposit,
                Amount = 50m
            });

        var result = await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(), 
                Type = TransactionType.Stake,
                Amount = 100m
            });

        Assert.Equal("rejected", result.Status);
        Assert.Equal(50m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task Idempotency_AcceptedTransaction_ShouldReturnAcceptedWithoutDoubleApplying()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        var transactionId = Guid.NewGuid();
        var request = new TransactionRequestDto
        {
            TransactionId = transactionId,
            Type = TransactionType.Deposit,
            Amount = 100m
        };

        var first = await _service.CreditTransaction(playerId, request);
        var second = await _service.CreditTransaction(playerId, request);

        Assert.Equal("accepted", first.Status);
        Assert.Equal("accepted", second.Status);
        Assert.Equal(100m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task Idempotency_RejectedTransaction_ShouldReturnRejectedOnReplay()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        var transactionId = Guid.NewGuid();
        var request = new TransactionRequestDto
        {
            TransactionId = transactionId,
            Type = TransactionType.Stake,
            Amount = 100m
        };

        var first = await _service.CreditTransaction(playerId, request);
        var second = await _service.CreditTransaction(playerId, request);

        Assert.Equal("rejected", first.Status);
        Assert.Equal("rejected", second.Status);
        Assert.Equal(0m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task GetTransactions_ShouldReturnAllPlayerTransactions()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        await _service.CreditTransaction(playerId, new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(), 
                Type = TransactionType.Deposit, 
                Amount = 100m
            });
        
        await _service.CreditTransaction(playerId, new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Stake,
                Amount = 30m
            });
        
        await _service.CreditTransaction(playerId, new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Win,
                Amount = 50m
            });

        var transactions = (await _service.GetTransactions(playerId)).ToList();

        Assert.Equal(3, transactions.Count);
        Assert.Equal(TransactionType.Deposit, transactions[0].Type);
        Assert.Equal(TransactionType.Stake, transactions[1].Type);
        Assert.Equal(TransactionType.Win, transactions[2].Type);
    }

    [Fact]
    public async Task Stake_ExactBalance_ShouldAccept()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        await _service.CreditTransaction(playerId,
            new TransactionRequestDto { TransactionId = Guid.NewGuid(), Type = TransactionType.Deposit, Amount = 50m });

        var result = await _service.CreditTransaction(playerId,
            new TransactionRequestDto { TransactionId = Guid.NewGuid(), Type = TransactionType.Stake, Amount = 50m });

        Assert.Equal("accepted", result.Status);
        Assert.Equal(0m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task CreditTransaction_NonExistentPlayer_ShouldThrow()
    {
        await Assert.ThrowsAsync<WalletNotFoundException>(
            () => _service.CreditTransaction(Guid.NewGuid(),
                new TransactionRequestDto { TransactionId = Guid.NewGuid(), Type = TransactionType.Deposit, Amount = 100m }));
    }
    
    [Fact]
    public async Task RejectedTransaction_ShouldBecomeAccepted_WhenBalanceIncreases()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        // Step 1: Deposit 100
        await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Deposit,
                Amount = 100m
            });
        Assert.Equal(100m, await _service.GetBalance(playerId));

        // Step 2: Stake 500 — rejected (insufficient funds)
        var stakeTxId = Guid.NewGuid();
        var stakeRequest = new TransactionRequestDto
        {
            TransactionId = stakeTxId,
            Type = TransactionType.Stake,
            Amount = 500m
        };

        var first = await _service.CreditTransaction(playerId, stakeRequest);
        Assert.Equal("rejected", first.Status);
        Assert.Equal(100m, await _service.GetBalance(playerId));

        // Step 3: Replay same stake — still rejected (balance unchanged)
        var second = await _service.CreditTransaction(playerId, stakeRequest);
        Assert.Equal("rejected", second.Status);
        Assert.Equal(100m, await _service.GetBalance(playerId));

        // Step 4: Deposit 500 — balance now 600
        await _service.CreditTransaction(playerId,
            new TransactionRequestDto
            {
                TransactionId = Guid.NewGuid(),
                Type = TransactionType.Deposit,
                Amount = 500m
            });
        Assert.Equal(600m, await _service.GetBalance(playerId));

        // Step 5: Replay same stake 500 — NOW ACCEPTED (balance sufficient), balance becomes 100
        var third = await _service.CreditTransaction(playerId, stakeRequest);
        Assert.Equal("accepted", third.Status);
        Assert.Equal(100m, await _service.GetBalance(playerId));

        // Step 6: Replay again — idempotent accepted, balance still 100
        var fourth = await _service.CreditTransaction(playerId, stakeRequest);
        Assert.Equal("accepted", fourth.Status);
        Assert.Equal(100m, await _service.GetBalance(playerId));
    }

    [Fact]
    public async Task Concurrent_Deposits_SamePlayer_ShouldSumExactly()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        const int count = 1000;
        const decimal amount = 1m;

        var tasks = Enumerable.Range(0, count).Select(_ =>
            _service.CreditTransaction(playerId,
                new TransactionRequestDto
                {
                    TransactionId = Guid.NewGuid(),
                    Type = TransactionType.Deposit,
                    Amount = amount
                }));
        await Task.WhenAll(tasks);

        Assert.Equal(count * amount, await _service.GetBalance(playerId));
        Assert.Equal(count, (await _service.GetTransactions(playerId)).Count());
    }
    
    [Fact]
    public async Task Concurrent_Transactions_DifferentPlayers_ShouldNotInterfere()
    {
        const int playerCount = 50;
        const int perPlayer = 100;

        var playerIds = Enumerable.Range(0, playerCount).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var id in playerIds)
        {
            await _service.RegisterWallet(id);
        }

        var playerTasks = playerIds.Select(async playerId =>
        {
            for (var i = 0; i < perPlayer; i++)
            {
                await _service.CreditTransaction(playerId,
                    new TransactionRequestDto
                    {
                        TransactionId = Guid.NewGuid(),
                        Type = TransactionType.Deposit,
                        Amount = 2m
                    });
            }
        });
        await Task.WhenAll(playerTasks);

        foreach (var playerId in playerIds)
        {
            Assert.Equal(perPlayer * 2m, await _service.GetBalance(playerId));
            Assert.Equal(perPlayer, (await _service.GetTransactions(playerId)).Count());
        }
    }
    
    [Fact]
    public async Task Concurrent_SameTransactionId_ShouldBeIdempotent()
    {
        var playerId = Guid.NewGuid();
        await _service.RegisterWallet(playerId);

        var txId = Guid.NewGuid();
        var request = new TransactionRequestDto
        {
            TransactionId = txId,
            Type = TransactionType.Deposit,
            Amount = 100m
        };

        const int attempts = 200;

        var tasks = Enumerable.Range(0, attempts).Select(_ =>
            _service.CreditTransaction(playerId, request));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal("accepted", r.Status));
        Assert.Equal(100m, await _service.GetBalance(playerId));
        Assert.Single(await _service.GetTransactions(playerId));
    }
}
