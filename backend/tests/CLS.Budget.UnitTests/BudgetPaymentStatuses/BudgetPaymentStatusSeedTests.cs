using CLS.Budget.Domain;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance.Seeding;
using FluentAssertions;

namespace CLS.Budget.UnitTests.BudgetPaymentStatuses;

public sealed class BudgetPaymentStatusSeedTests
{
    [Fact]
    public void ScheduledOnline_HasStableId()
    {
        BudgetPaymentStatusIds.ScheduledOnline.Should().Be(7);
    }

    [Fact]
    public void GetBudgetPaymentStatuses_IncludesScheduledOnline()
    {
        var statuses = LookupDataSeed.GetBudgetPaymentStatuses().ToList();

        var scheduledOnline = statuses.Should()
            .ContainSingle(s => s.BudgetPaymentStatusId == BudgetPaymentStatusIds.ScheduledOnline)
            .Subject;

        scheduledOnline.Name.Should().Be("Scheduled Online");
        scheduledOnline.Description.Should().Be("Scheduled for online payment");
    }

    [Fact]
    public void GetBudgetPaymentStatuses_HasUniqueIdsAndNames()
    {
        var statuses = LookupDataSeed.GetBudgetPaymentStatuses().ToList();

        statuses.Select(s => s.BudgetPaymentStatusId).Should().OnlyHaveUniqueItems();
        statuses.Select(s => s.Name).Should().OnlyHaveUniqueItems();
        statuses.Should().HaveCountGreaterThanOrEqualTo(7);
    }

    [Theory]
    [InlineData(BudgetPaymentStatusIds.Pending, "Pending")]
    [InlineData(BudgetPaymentStatusIds.Scheduled, "Scheduled")]
    [InlineData(BudgetPaymentStatusIds.Paid, "Paid")]
    [InlineData(BudgetPaymentStatusIds.Failed, "Failed")]
    [InlineData(BudgetPaymentStatusIds.Overdue, "Overdue")]
    [InlineData(BudgetPaymentStatusIds.Unassigned, "Unassigned")]
    [InlineData(BudgetPaymentStatusIds.ScheduledOnline, "Scheduled Online")]
    public void GetBudgetPaymentStatuses_MapsKnownIdsToNames(int id, string name)
    {
        LookupDataSeed.GetBudgetPaymentStatuses()
            .Should()
            .ContainEquivalentOf(new BudgetPaymentStatus
            {
                BudgetPaymentStatusId = id,
                Name = name
            }, options => options
                .Including(s => s.BudgetPaymentStatusId)
                .Including(s => s.Name));
    }
}
