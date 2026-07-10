using CLS.Budget.Application.Budgets;
using FluentAssertions;

namespace CLS.Budget.UnitTests.Budgets;

public sealed class BudgetPaymentDateMapperTests
{
    [Fact]
    public void MapToBudgetPeriod_PreservesDayOfMonth()
    {
        var source = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var targetStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var mapped = BudgetPaymentDateMapper.MapToBudgetPeriod(source, targetStart);

        mapped.Should().Be(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void MapToBudgetPeriod_ClampsToLastDayOfShorterMonth()
    {
        var source = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var targetStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var mapped = BudgetPaymentDateMapper.MapToBudgetPeriod(source, targetStart);

        mapped.Should().Be(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));
    }
}
