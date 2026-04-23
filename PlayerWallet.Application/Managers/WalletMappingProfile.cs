using AutoMapper;
using PlayerWallet.Application.Models;
using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Application.Managers;

public class WalletMappingProfile : Profile
{
    public WalletMappingProfile()
    {
        CreateMap<Wallet, WalletDto>().ReverseMap();
        CreateMap<WalletTransaction, WalletTransactionDto>().ReverseMap();
    }
}