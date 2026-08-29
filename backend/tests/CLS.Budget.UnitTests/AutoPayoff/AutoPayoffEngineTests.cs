using CLS.Budget.Domain.AutoPayoff;
using FluentAssertions;

namespace CLS.Budget.UnitTests.AutoPayoff;

public sealed class AutoPayoffEngineTests
{
    private readonly AutoPayoffEngine _sut = new();
    private static readonly DateOnly AsOf = new(2026, 8, 15);

    [Fact]
    public void Excludes_Status_Category_And_Account()
    {
        var accounts = new[]
        {
            Card(1, "Visa", categoryId: 1, balance: 1000, statusId: 3, statusName: "Paid"),
            Card(2, "Loan", categoryId: 2, balance: 4000),
            Card(3, "Mortgage", categoryId: 3, balance: 200000),
            Card(4, "Store card", categoryId: 1, balance: 200),
        };

        var plan = _sut.BuildPlan(
            accounts,
            new AutoPayoffRules(
                ExcludeStatusIds: [3],
                ExcludeCategoryIds: [2],
                ExcludeAccountIds: [3],
                TargetCategoryIds: [],
                TargetByPayoffDate: false),
            extraPayment: 0,
            asOf: AsOf);

        plan.Queue.Should().ContainSingle(item => item.AccountId == 4);
        plan.Excluded.Select(x => x.Reason).Should().BeEquivalentTo(
        [
            "Status excluded (Paid)",
            "Category excluded",
            "Account excluded",
        ]);
    }

    [Fact]
    public void TargetCategories_ComeFirst_InConfiguredOrder()
    {
        var accounts = new[]
        {
            Card(1, "Utility", categoryId: 4, balance: 120),
            Card(2, "Visa", categoryId: 1, balance: 900),
            Card(3, "Mortgage", categoryId: 3, balance: 180000),
        };

        var plan = _sut.BuildPlan(
            accounts,
            new AutoPayoffRules(
                [],
                [],
                [],
                TargetCategoryIds: [3, 1],
                TargetByPayoffDate: false),
            extraPayment: 0,
            asOf: AsOf);

        plan.Queue.Select(x => x.AccountId).Should().Equal(3, 2, 1);
        plan.Queue[0].Reason.Should().Be("Target category first");
        plan.Queue[1].Reason.Should().Be("Target category first");
        plan.Queue[2].Reason.Should().Be("Eligible after target categories");
    }

    [Fact]
    public void TargetByPayoffDate_OrdersRemainingBySoonestDate()
    {
        var accounts = new[]
        {
            Card(1, "Later", categoryId: 1, balance: 500, paymentDay: 28),
            Card(2, "Sooner", categoryId: 1, balance: 800, paymentDay: 18),
        };

        var plan = _sut.BuildPlan(
            accounts,
            new AutoPayoffRules([], [], [], [], TargetByPayoffDate: true),
            extraPayment: 0,
            asOf: AsOf);

        plan.Queue.Select(x => x.AccountId).Should().Equal(2, 1);
        plan.Queue[0].Reason.Should().Be("Ordered by payoff / due date");
    }

    [Fact]
    public void ExtraPayment_Waterfalls_OntoFirstThenNext()
    {
        var accounts = new[]
        {
            Card(1, "First", categoryId: 1, balance: 100),
            Card(2, "Second", categoryId: 1, balance: 400),
        };

        var plan = _sut.BuildPlan(
            accounts,
            new AutoPayoffRules([], [], [], TargetCategoryIds: [1], TargetByPayoffDate: false),
            extraPayment: 250,
            asOf: AsOf);

        plan.Queue[0].ExtraAllocated.Should().Be(100);
        plan.Queue[1].ExtraAllocated.Should().Be(150);
        plan.ExtraAllocated.Should().Be(250);
        plan.ExtraRemaining.Should().Be(0);
    }

    [Fact]
    public void Skips_PaidOff_And_PayoffAnalysisFlag()
    {
        var accounts = new[]
        {
            Card(1, "Done", categoryId: 1, balance: 0, isPaidOff: true),
            Card(2, "Plan only", categoryId: 1, balance: 500, includeInPayoff: false),
            Card(3, "Keep", categoryId: 1, balance: 200),
        };

        var plan = _sut.BuildPlan(
            accounts,
            new AutoPayoffRules([], [], [], [], false),
            0,
            AsOf);

        plan.Queue.Should().ContainSingle(item => item.AccountId == 3);
        plan.Excluded.Should().HaveCount(2);
    }

    private static AutoPayoffAccountInput Card(
        int id,
        string name,
        int categoryId,
        decimal balance,
        int? statusId = null,
        string? statusName = null,
        int? paymentDay = null,
        bool isPaidOff = false,
        bool includeInPayoff = true) =>
        new(
            id,
            name,
            categoryId,
            $"Category {categoryId}",
            balance,
            MonthlyPayment: 50,
            PaymentDay: paymentDay,
            NextPaymentDate: null,
            PaymentStatusId: statusId,
            PaymentStatusName: statusName,
            IsPaidOff: isPaidOff,
            IncludeInPayoffAnalysis: includeInPayoff);
}
