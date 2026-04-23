using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using PlayerWallet.Api.Middleware;
using PlayerWallet.Application.Interfaces;
using PlayerWallet.Application.Managers;
using PlayerWallet.Domain.Interfaces;
using PlayerWallet.Application.Services;
using PlayerWallet.Domain.Data;
using PlayerWallet.Domain.Repositories;
using PlayerWallet.Infrastructure.Redis;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Scalar.AspNetCore;
using StackExchange.Redis;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    logger.Info("Starting PlayerWallet.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi();
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<WalletMappingProfile>());

    var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        // Redis-backed distributed lock + balance cache
        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        builder.Services.AddSingleton(sp =>
            RedLockFactory.Create(new List<RedLockMultiplexer> { new(multiplexer) }));
        builder.Services.AddSingleton<ISemaphoreSlimManager, RedisDistributedLockManager>();
        builder.Services.AddSingleton<IBalanceCacheService, RedisBalanceCacheService>();
    }
    else
    {
        // In-process fallback (single node)
        builder.Services.AddSingleton<ISemaphoreSlimManager, SemaphoreSlimManager>();
        builder.Services.AddSingleton<IBalanceCacheService, NoOpBalanceCacheService>();
    }

    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        builder.Services.AddDbContext<WalletDbContext>(options => options.UseInMemoryDatabase("PlayerWalletDb"));
        builder.Services.AddScoped<IWalletRepository, WalletInMemoryRepository>();
        builder.Services.AddScoped<ITransactionRepository, TransactionInMemoryRepository>();
        builder.Services.AddScoped<IWalletManager, WalletManager>();
        builder.Services.AddScoped<ITransactionManager, TransactionManager>();
        builder.Services.AddScoped<IWalletService, WalletService>();
    }
    else
    {
        builder.Services.AddSingleton<IWalletRepository, WalletRepository>();
        builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();
        builder.Services.AddSingleton<IWalletManager, WalletManager>();
        builder.Services.AddSingleton<ITransactionManager, TransactionManager>();
        builder.Services.AddSingleton<IWalletService, WalletService>();
    }
    
    var app = builder.Build();
    
    app.UseMiddleware<ExceptionMiddleware>();
    
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "PlayerWallet.Api terminated unexpectedly");
    throw;
}
finally
{
    LogManager.Shutdown();
}
