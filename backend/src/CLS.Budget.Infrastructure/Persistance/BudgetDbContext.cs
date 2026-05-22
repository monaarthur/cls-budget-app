using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance.Seeding;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Persistance;

public class BudgetDbContext(DbContextOptions<BudgetDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountCategory> AccountCategories => Set<AccountCategory>();
    public DbSet<BudgetTemplate> BudgetTemplates => Set<BudgetTemplate>();
    public DbSet<BudgetModel> Budgets => Set<BudgetModel>();
    public DbSet<PaymentSource> PaymentSources => Set<PaymentSource>();
    public DbSet<BudgetPayment> BudgetPayments => Set<BudgetPayment>();
    public DbSet<BudgetPaymentStatus> BudgetPaymentStatuses => Set<BudgetPaymentStatus>();
    public DbSet<CreditCardDetail> CreditCardDetails => Set<CreditCardDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountCategory>(e =>
        {
            e.HasKey(x => x.AccountCategoryId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(x => x.AccountId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Number).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Balance).IsRequired();
            e.Property(x => x.Limit);
            e.Property(x => x.MonthlyPayment);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Url).HasMaxLength(500);
            e.Property(x => x.Username).HasMaxLength(200);
            e.Property(x => x.Password).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasIndex(x => x.Number);
            e.HasOne(x => x.CreditCardDetail)
                .WithOne(x => x.Account)
                .HasForeignKey<CreditCardDetail>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditCardDetail>(e =>
        {
            e.ToTable("CreditCardDetail");
            e.HasKey(x => x.CreditCardDetailId);
            e.Property(x => x.InterestRate).HasColumnType("numeric(8,4)");
            e.Property(x => x.Limit).HasColumnType("numeric(18,2)");
            e.Property(x => x.CashOutInterestRate).HasColumnType("numeric(8,4)");
            e.HasIndex(x => x.AccountId).IsUnique();
        });

        modelBuilder.Entity<BudgetTemplate>(e =>
        {
            e.HasKey(x => x.BudgetTemplateId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<BudgetModel>(e =>
        {
            e.ToTable("Budget");
            e.HasKey(x => x.BudgetId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.StartPeriod).IsRequired();
            e.Property(x => x.EndPeriod).IsRequired();
            e.Property(x => x.AccountIds);
            e.HasOne(x => x.BudgetTemplate)
                .WithMany()
                .HasForeignKey(x => x.BudgetTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentSource>(e =>
        {
            e.ToTable("PaymentSource");
            e.HasKey(x => x.PaymentSourceId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<BudgetPaymentStatus>(e =>
        {
            e.ToTable("BudgetPaymentStatus");
            e.HasKey(x => x.BudgetPaymentStatusId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<BudgetPayment>(e =>
        {
            e.ToTable("BudgetPayments");
            e.HasKey(x => x.BudgetPaymentId);
            e.Property(x => x.PaymentMade).HasColumnType("numeric(18,2)");
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.HasOne(x => x.BudgetPaymentStatus)
                .WithMany()
                .HasForeignKey(x => x.BudgetPaymentStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BudgetModel>()
                .WithMany(x => x.BudgetPayments)
                .HasForeignKey(x => x.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PaymentSource)
                .WithMany()
                .HasForeignKey(x => x.PaymentSourceId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.BudgetId, x.AccountId, x.PaymentDate });
        });

        LookupDataSeed.Apply(modelBuilder);
    }
}
