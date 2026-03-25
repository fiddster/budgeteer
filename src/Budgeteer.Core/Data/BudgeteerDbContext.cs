using Budgeteer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Data;

public class BudgeteerDbContext(DbContextOptions<BudgeteerDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ColumnMapping> ColumnMappings => Set<ColumnMapping>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ColumnMapping>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.HasOne(e => e.Account)
                  .WithOne(a => a.ColumnMapping)
                  .HasForeignKey<ColumnMapping>(e => e.AccountId);
        });

        modelBuilder.Entity<BudgetCategory>(entity =>
        {
            entity.HasKey(e => new { e.BudgetId, e.CategoryId });
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("TEXT");
            entity.Property(e => e.Balance).HasColumnType("TEXT");
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(e => e.SpendingLimit).HasColumnType("TEXT");
        });
    }
}
