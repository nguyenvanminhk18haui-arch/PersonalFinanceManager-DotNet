using Microsoft.EntityFrameworkCore;
using ThuChiCaNhan.Models;

namespace ThuChiCaNhan.Data;

public sealed class TrackerDbContext : DbContext
{
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
    public DbSet<ExpenseCategory> Categories => Set<ExpenseCategory>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<CategoryBudget> Budgets => Set<CategoryBudget>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Environment.GetEnvironmentVariable("THUCHI_SQL_CONNECTION")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=ThuChiCaNhan;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExpenseCategory>()
            .HasIndex(category => new { category.Name, category.Type })
            .IsUnique();

        modelBuilder.Entity<TransactionRecord>()
            .Property(transaction => transaction.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TransactionRecord>()
            .HasOne(transaction => transaction.Category)
            .WithMany(category => category.Transactions)
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TransactionRecord>()
            .HasOne(transaction => transaction.Wallet)
            .WithMany(wallet => wallet.Transactions)
            .HasForeignKey(transaction => transaction.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TransactionRecord>()
            .HasOne(transaction => transaction.RecurringTransaction)
            .WithMany()
            .HasForeignKey(transaction => transaction.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Wallet>().Property(wallet => wallet.Name).HasMaxLength(80);
        modelBuilder.Entity<Wallet>().Property(wallet => wallet.Type).HasMaxLength(40);
        modelBuilder.Entity<Wallet>().HasIndex(wallet => wallet.Name).IsUnique();
        modelBuilder.Entity<CategoryBudget>()
            .HasIndex(budget => new { budget.CategoryId, budget.Month })
            .IsUnique();
        modelBuilder.Entity<CategoryBudget>().Property(budget => budget.Limit).HasPrecision(18, 2);

        modelBuilder.Entity<RecurringTransaction>().Property(schedule => schedule.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<RecurringTransaction>()
            .HasOne(schedule => schedule.Category)
            .WithMany()
            .HasForeignKey(schedule => schedule.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RecurringTransaction>()
            .HasOne(schedule => schedule.Wallet)
            .WithMany()
            .HasForeignKey(schedule => schedule.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
