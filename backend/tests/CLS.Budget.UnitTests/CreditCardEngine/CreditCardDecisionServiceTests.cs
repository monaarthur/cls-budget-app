using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.CreditCardEngine;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using CLS.Budget.Application.CreditCards;
using CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;
using CLS.Budget.Domain.CreditCardEngine.CashFlow;
using CLS.Budget.Domain.CreditCardEngine.Forecast;
using CLS.Budget.Domain.CreditCardEngine.Interest;
using CLS.Budget.Domain.CreditCardEngine.Payoff;
using CLS.Budget.Domain.CreditCardEngine.Utilization;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class CreditCardDecisionServiceTests
{
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly CreditCardDecisionService _sut;

    public CreditCardDecisionServiceTests()
    {
        var payoff = new PayoffStrategyEngine();
        _sut = new CreditCardDecisionService(
            _accounts.Object,
            Mock.Of<IForecastScenarioRepository>(),
            Mock.Of<ISavedPayoffPlanRepository>(),
            Mock.Of<IActivePayoffPlanRepository>(),
            new InterestCalculationEngine(),
            new UtilizationEngine(),
            payoff,
            new BalanceTransferEngine(),
            new CashFlowEngine(),
            new ForecastEngine(payoff));
    }

    [Fact]
    public async Task ComparePayoffPlansAsync_ExcludesCardsMarkedNotIncludedInPayoffAnalysis()
    {
        _accounts
            .Setup(r => r.GetByCategoryAsync(CreditCardCategory.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateCard(1, "Chase", balance: 1000m, apr: 20m, min: 50m, includeInPayoff: true),
                CreateCard(2, "Creditor Plan", balance: 5000m, apr: 0m, min: 200m, includeInPayoff: false),
                CreateCard(3, "Discover", balance: 800m, apr: 18m, min: 40m, includeInPayoff: true),
            ]);

        var result = await _sut.ComparePayoffPlansAsync(new ComparePayoffPlansRequest
        {
            TotalMonthlyDebtPayment = 200m,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Result.StartingDebt.Should().Be(1800m);
        result.Data.Result.Strategies.Should().NotBeEmpty();
        result.Data.Result.Strategies
            .SelectMany(s => s.CardOrder)
            .Select(c => c.Name)
            .Distinct()
            .Should()
            .BeEquivalentTo(["Chase", "Discover"]);
        result.Data.Warnings.Should().Contain(w => w.Contains("Creditor Plan") && w.Contains("excluded"));
    }

    [Fact]
    public async Task ComparePayoffPlansAsync_TreatsMissingCreditCardDetailAsIncluded()
    {
        _accounts
            .Setup(r => r.GetByCategoryAsync(CreditCardCategory.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Account
                {
                    AccountId = 10,
                    Name = "Legacy Card",
                    Balance = 500m,
                    MonthlyPayment = 50m,
                    IsPaidOff = false,
                    AccountCategoryId = CreditCardCategory.CategoryId,
                    CreditCardDetail = null
                }
            ]);

        var result = await _sut.ComparePayoffPlansAsync(new ComparePayoffPlansRequest
        {
            TotalMonthlyDebtPayment = 50m,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        result.Success.Should().BeTrue();
        result.Data!.Result.StartingDebt.Should().Be(500m);
        result.Data.Result.Strategies
            .SelectMany(s => s.CardOrder)
            .Should()
            .Contain(c => c.Name == "Legacy Card");
    }

    [Fact]
    public async Task ComparePayoffPlansAsync_FailsWhenAllCardsWithBalanceAreExcluded()
    {
        _accounts
            .Setup(r => r.GetByCategoryAsync(CreditCardCategory.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateCard(1, "Plan Only", balance: 2000m, apr: 0m, min: 100m, includeInPayoff: false),
            ]);

        var result = await _sut.ComparePayoffPlansAsync(new ComparePayoffPlansRequest
        {
            TotalMonthlyDebtPayment = 100m
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("exclusions"));
    }

    [Fact]
    public async Task GetUtilizationSummaryAsync_StillIncludesExcludedPayoffCards()
    {
        _accounts
            .Setup(r => r.GetByCategoryAsync(CreditCardCategory.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateCard(1, "Chase", balance: 500m, apr: 20m, min: 50m, includeInPayoff: true, limit: 1000m),
                CreateCard(2, "Plan Card", balance: 500m, apr: 0m, min: 50m, includeInPayoff: false, limit: 1000m),
            ]);

        var result = await _sut.GetUtilizationSummaryAsync();

        result.Success.Should().BeTrue();
        result.Data!.Result.Cards.Should().HaveCount(2);
        result.Data.Result.TotalBalances.Should().Be(1000m);
    }

    private static Account CreateCard(
        int id,
        string name,
        decimal balance,
        decimal apr,
        decimal min,
        bool includeInPayoff,
        decimal limit = 5000m) =>
        new()
        {
            AccountId = id,
            Name = name,
            Balance = balance,
            Limit = limit,
            MonthlyPayment = min,
            IsPaidOff = false,
            AccountCategoryId = CreditCardCategory.CategoryId,
            CreditCardDetail = new CreditCardDetail
            {
                AccountId = id,
                InterestRate = apr,
                IncludeInPayoffAnalysis = includeInPayoff
            }
        };
}
