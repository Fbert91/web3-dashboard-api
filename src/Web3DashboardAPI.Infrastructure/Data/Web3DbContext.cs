using Microsoft.EntityFrameworkCore;
using Web3DashboardAPI.Domain.Entities;

namespace Web3DashboardAPI.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for Web3 operations.
/// </summary>
public class Web3DbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the Web3DbContext.
    /// </summary>
    public Web3DbContext(DbContextOptions<Web3DbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Wallets database set.
    /// </summary>
    public DbSet<Wallet> Wallets { get; set; }

    /// <summary>
    /// Token balances database set.
    /// </summary>
    public DbSet<TokenBalance> TokenBalances { get; set; }

    /// <summary>
    /// Transactions database set.
    /// </summary>
    public DbSet<Transaction> Transactions { get; set; }

    /// <summary>
    /// Configures the model on model creation.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wallet configuration
        modelBuilder.Entity<Wallet>()
            .HasKey(w => w.Id);

        modelBuilder.Entity<Wallet>()
            .Property(w => w.Address)
            .IsRequired();

        modelBuilder.Entity<Wallet>()
            .HasIndex(w => w.Address)
            .IsUnique();

        // TokenBalance configuration
        modelBuilder.Entity<TokenBalance>()
            .HasKey(tb => tb.Id);

        modelBuilder.Entity<TokenBalance>()
            .HasOne(tb => tb.Wallet)
            .WithMany(w => w.TokenBalances)
            .HasForeignKey(tb => tb.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TokenBalance>()
            .HasIndex(tb => new { tb.WalletId, tb.ContractAddress })
            .IsUnique();

        // Transaction configuration
        modelBuilder.Entity<Transaction>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionHash)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.WalletId);
    }
}
