using CLS.Budget.Domain.CreditCardEngine.Loan;
using FluentAssertions;
using Xunit;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class LoanRepaymentScheduleBuilderTests
{
    [Fact]
    public void Personal_Amortizing_ComputesPaymentInterestAndSchedule()
    {
        var result = LoanRepaymentScheduleBuilder.Build(new LoanScheduleRequest(
            LoanType.Personal,
            Amount: 10_000m,
            AnnualPercentageRate: 12m,
            TermMonths: 12));

        result.IsValid.Should().BeTrue();
        result.MonthlyPayment.Should().BeGreaterThan(800m);
        result.MonthsToPayoff.Should().Be(12);
        result.Schedule.Should().HaveCount(12);
        result.Schedule.Last().EndingBalance.Should().Be(0m);
        result.TotalInterest.Should().BeGreaterThan(0m);
        result.TotalPaid.Should().Be(result.TotalInterest + 10_000m);
    }

    [Fact]
    public void Family_ZeroApr_HasNoInterest()
    {
        var result = LoanRepaymentScheduleBuilder.Build(new LoanScheduleRequest(
            LoanType.Family,
            Amount: 1200m,
            AnnualPercentageRate: 0m,
            FixedMonthlyPayment: 100m));

        result.IsValid.Should().BeTrue();
        result.TotalInterest.Should().Be(0m);
        result.MonthsToPayoff.Should().Be(12);
        result.Schedule.Last().EndingBalance.Should().Be(0m);
    }

    [Fact]
    public void Heloc_InterestOnlyThenAmortizes()
    {
        var result = LoanRepaymentScheduleBuilder.Build(new LoanScheduleRequest(
            LoanType.Heloc,
            Amount: 10_000m,
            AnnualPercentageRate: 12m,
            TermMonths: 24,
            InterestOnlyMonths: 6));

        result.IsValid.Should().BeTrue();
        result.Phase2MonthlyPayment.Should().NotBeNull();
        result.Phase2MonthlyPayment.Should().BeGreaterThan(result.MonthlyPayment);
        result.Schedule.Take(6).Should().OnlyContain(m => m.Principal == 0m);
        result.Schedule.Skip(6).Should().Contain(m => m.Principal > 0m);
        result.Schedule.Last().EndingBalance.Should().Be(0m);
        result.TotalInterest.Should().BeGreaterThan(0m);
    }
}
