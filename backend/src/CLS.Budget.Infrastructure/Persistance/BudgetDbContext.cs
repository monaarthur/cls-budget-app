using CLS.Budget.Application.Abstractions;
using CLS.Budget.Domain;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance.Seeding;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Persistance;

public class BudgetDbContext(DbContextOptions<BudgetDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountCategory> AccountCategories => Set<AccountCategory>();
    public DbSet<BudgetTemplate> BudgetTemplates => Set<BudgetTemplate>();
    public DbSet<BudgetModel> Budgets => Set<BudgetModel>();
    public DbSet<PaymentSource> PaymentSources => Set<PaymentSource>();
    public DbSet<BudgetPayment> BudgetPayments => Set<BudgetPayment>();
    public DbSet<BudgetIncome> BudgetIncomes => Set<BudgetIncome>();
    public DbSet<BudgetPaymentStatus> BudgetPaymentStatuses => Set<BudgetPaymentStatus>();
    public DbSet<CreditCardDetail> CreditCardDetails => Set<CreditCardDetail>();
    public DbSet<PayFrequencyType> PayFrequencyTypes => Set<PayFrequencyType>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<PaySchedule> PaySchedules => Set<PaySchedule>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
            e.Property(x => x.PaymentDay);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Url).HasMaxLength(500);
            e.Property(x => x.Username).HasMaxLength(200);
            e.Property(x => x.Password).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.HasIndex(x => x.Number);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.CreditCardDetail)
                .WithOne(x => x.Account)
                .HasForeignKey<CreditCardDetail>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<CreditCardDetail>(e =>
        {
            e.ToTable("CreditCardDetail");
            e.HasKey(x => x.CreditCardDetailId);
            e.Property(x => x.InterestRate).HasColumnType("numeric(8,4)");
            e.Property(x => x.Limit).HasColumnType("numeric(18,2)");
            e.Property(x => x.CashOutInterestRate).HasColumnType("numeric(8,4)");
            e.HasIndex(x => x.AccountId).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
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
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.AccountIds);
            e.HasOne(x => x.BudgetTemplate)
                .WithMany()
                .HasForeignKey(x => x.BudgetTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PaySchedule)
                .WithMany()
                .HasForeignKey(x => x.PayScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<PayFrequencyType>(e =>
        {
            e.ToTable("PayFrequencyType");
            e.HasKey(x => x.PayFrequencyTypeId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<IncomeSource>(e =>
        {
            e.ToTable("IncomeSource");
            e.HasKey(x => x.IncomeSourceId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<PaySchedule>(e =>
        {
            e.ToTable("PaySchedule");
            e.HasKey(x => x.PayScheduleId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.IncomeSource)
                .WithMany()
                .HasForeignKey(x => x.IncomeSourceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PayFrequencyType)
                .WithMany()
                .HasForeignKey(x => x.PayFrequencyTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
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
            e.HasOne(x => x.IncomeSource)
                .WithMany()
                .HasForeignKey(x => x.IncomeSourceId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.BudgetId, x.AccountId, x.PaymentDate });
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<BudgetIncome>(e =>
        {
            e.ToTable("BudgetIncome");
            e.HasKey(x => x.BudgetIncomeId);
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.IncomeSource)
                .WithMany()
                .HasForeignKey(x => x.IncomeSourceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BudgetModel>()
                .WithMany()
                .HasForeignKey(x => x.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.BudgetId, x.IncomeSourceId, x.ReceivedDate });
            e.HasIndex(x => x.TenantId);
            e.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenant");
            e.HasKey(x => x.TenantId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasData(new Tenant
            {
                TenantId = SeedTenant.DefaultTenantId,
                Name = SeedTenant.DefaultTenantName,
                IsActive = true,
                CreatedAt = SeedTenant.DefaultCreatedAt
            });
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("AppUser");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.HasOne(x => x.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshToken");
            e.HasKey(x => x.RefreshTokenId);
            e.Property(x => x.TokenHash).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        LookupDataSeed.Apply(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTenant()
    {
        var tenantId = _tenantContext.TenantId;
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}
