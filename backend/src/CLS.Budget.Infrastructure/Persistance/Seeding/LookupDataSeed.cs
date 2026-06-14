using CLS.Budget.Domain;
using CLS.Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Persistance.Seeding;

/// <summary>
/// Reference data for account categories and budget templates.
/// Ids are fixed so FKs and <see cref="CLS.Budget.Application.CreditCards.CreditCardCategory"/> stay stable.
/// </summary>
public static class LookupDataSeed
{
    public const int DefaultBudgetTemplateId = 1;
    public const int DefaultIncomeSourceId = 1;
    public const int DefaultPayScheduleId = 1;

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountCategory>().HasData(GetAccountCategories());
        modelBuilder.Entity<BudgetTemplate>().HasData(GetBudgetTemplates());
        modelBuilder.Entity<BudgetPaymentStatus>().HasData(GetBudgetPaymentStatuses());
        modelBuilder.Entity<PaymentSource>().HasData(GetPaymentSources());
        modelBuilder.Entity<PayFrequencyType>().HasData(GetPayFrequencyTypes());
        modelBuilder.Entity<IncomeSource>().HasData(GetIncomeSources());
        modelBuilder.Entity<PaySchedule>().HasData(GetPaySchedules());
    }

    public static IEnumerable<AccountCategory> GetAccountCategories() =>
    [
        new AccountCategory
        {
            AccountCategoryId = 1,
            Name = "Credit Card",
            Description = "Revolving credit accounts"
        },
        new AccountCategory
        {
            AccountCategoryId = 2,
            Name = "Loan",
            Description = "Installment loans"
        },
        new AccountCategory
        {
            AccountCategoryId = 3,
            Name = "Mortgage",
            Description = "Home mortgage accounts"
        },
        new AccountCategory
        {
            AccountCategoryId = 4,
            Name = "Utility",
            Description = "Utility and service bills"
        },
        new AccountCategory
        {
            AccountCategoryId = 5,
            Name = "Subscription",
            Description = "Recurring subscriptions"
        },
        new AccountCategory
        {
            AccountCategoryId = 6,
            Name = "Savings",
            Description = "Savings accounts"
        },
        new AccountCategory
        {
            AccountCategoryId = 7,
            Name = "Checking",
            Description = "Checking accounts"
        }
    ];

    public static IEnumerable<BudgetTemplate> GetBudgetTemplates() =>
    [
        new BudgetTemplate
        {
            BudgetTemplateId = DefaultBudgetTemplateId,
            Name = "Monthly Household Budget",
            Description = "Default template for monthly budget planning",
            AccountIds = null
        }
    ];

    public static IEnumerable<BudgetPaymentStatus> GetBudgetPaymentStatuses() =>
    [
        new BudgetPaymentStatus { BudgetPaymentStatusId = 1, Name = "Pending", Description = "Not yet paid" },
        new BudgetPaymentStatus { BudgetPaymentStatusId = 2, Name = "Scheduled", Description = "Scheduled for payment" },
        new BudgetPaymentStatus { BudgetPaymentStatusId = 3, Name = "Paid", Description = "Payment completed" },
        new BudgetPaymentStatus { BudgetPaymentStatusId = 4, Name = "Failed", Description = "Payment attempt failed" },
        new BudgetPaymentStatus { BudgetPaymentStatusId = 5, Name = "Overdue", Description = "Past due and not paid" }
    ];

    public static IEnumerable<PaymentSource> GetPaymentSources() =>
    [
        new PaymentSource
        {
            PaymentSourceId = 1,
            Name = "HVCU",
            Description = "Hudson Valley Credit Union checking"
        },
        new PaymentSource
        {
            PaymentSourceId = 2,
            Name = "HVCU CC",
            Description = "HVCU credit card used for payments"
        },
        new PaymentSource
        {
            PaymentSourceId = 3,
            Name = "AE",
            Description = "Alternate / American Express funding account"
        }
    ];

    public static IEnumerable<PayFrequencyType> GetPayFrequencyTypes() =>
    [
        new PayFrequencyType
        {
            PayFrequencyTypeId = PayFrequencyTypeIds.Weekly,
            Name = "Weekly",
            Description = "Paid every 7 days on a fixed weekday"
        },
        new PayFrequencyType
        {
            PayFrequencyTypeId = PayFrequencyTypeIds.BiWeekly,
            Name = "BiWeekly",
            Description = "Paid every 14 days on a fixed weekday"
        },
        new PayFrequencyType
        {
            PayFrequencyTypeId = PayFrequencyTypeIds.SemiMonthly,
            Name = "SemiMonthly",
            Description = "Paid twice per month on fixed calendar days"
        }
    ];

    public static IEnumerable<IncomeSource> GetIncomeSources() =>
    [
        new IncomeSource
        {
            IncomeSourceId = DefaultIncomeSourceId,
            Name = "Job Income",
            IsActive = true
        },
        new IncomeSource
        {
            IncomeSourceId = 2,
            Name = "Credit Cards",
            IsActive = true
        },
        new IncomeSource
        {
            IncomeSourceId = 3,
            Name = "Business Income",
            IsActive = true
        }
    ];

    public static IEnumerable<PaySchedule> GetPaySchedules() =>
    [
        new PaySchedule
        {
            PayScheduleId = DefaultPayScheduleId,
            TenantId = SeedTenant.DefaultTenantId,
            IncomeSourceId = DefaultIncomeSourceId,
            PayFrequencyTypeId = PayFrequencyTypeIds.SemiMonthly,
            Name = "Twice monthly (1st & 15th)",
            SemiMonthlyDay1 = 1,
            SemiMonthlyDay2 = 15,
            IsDefault = true,
            IsActive = true
        }
    ];
}
