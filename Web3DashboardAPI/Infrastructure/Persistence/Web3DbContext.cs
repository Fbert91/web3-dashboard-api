using Microsoft.EntityFrameworkCore;
using Web3DashboardAPI.Domain.Models;

namespace Web3DashboardAPI.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for Web3 Dashboard
/// </summary>
public class Web3DbContext : DbContext
{
    public Web3DbContext(DbContextOptions<Web3DbContext> options) : base(options) { }

    public DbSet<WalletRecord> Wallets { get; set; } = null!;
    public DbSet<TransactionRecord> Transactions { get; set; } = null!;
    public DbSet<TokenRecord> Tokens { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Use SQLite database file in project directory
            optionsBuilder.UseSqlite("Data Source=web3dashboard.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wallet configuration
        modelBuilder.Entity<WalletRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(42);
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Address).IsUnique();
        });

        // Transaction configuration
        modelBuilder.Entity<TransactionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Hash).IsRequired().HasMaxLength(66);
            entity.Property(e => e.From).IsRequired().HasMaxLength(42);
            entity.Property(e => e.To).HasMaxLength(42);
            entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Hash).IsUnique();
        });

        // Token configuration
        modelBuilder.Entity<TokenRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContractAddress).IsRequired().HasMaxLength(42);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.ContractAddress).IsUnique();
        });
    }
}
