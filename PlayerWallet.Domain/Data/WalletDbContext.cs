using Microsoft.EntityFrameworkCore;
using PlayerWallet.Domain.Entities;

namespace PlayerWallet.Domain.Data;

public class WalletDbContext : DbContext
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();

    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.PlayerId);
            e.Property(w => w.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.TransactionId);
            e.HasIndex(t => t.PlayerId);
            e.Property(t => t.Amount).HasPrecision(18, 2);
        });
    }
}
