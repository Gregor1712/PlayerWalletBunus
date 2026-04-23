using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using PlayerWallet.Application.Managers;

namespace PlayerWallet.Tests;

internal static class TestMapperFactory
{
    private static readonly IMapper SharedMapper = new MapperConfiguration(
        cfg => cfg.AddProfile<WalletMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    public static IMapper Create() => SharedMapper;
}