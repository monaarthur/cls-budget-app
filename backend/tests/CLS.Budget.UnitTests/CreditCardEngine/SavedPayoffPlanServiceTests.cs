using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.CreditCardEngine;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using CLS.Budget.Application.CreditCardEngine.Validators;
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

public sealed class SavedPayoffPlanServiceTests
{
    [Fact]
    public async Task CreateSavedPayoffPlanAsync_PersistsStepsAndReturnsDto()
    {
        var repo = new Mock<ISavedPayoffPlanRepository>();
        SavedPayoffPlan? added = null;
        repo.Setup(r => r.AddAsync(It.IsAny<SavedPayoffPlan>(), It.IsAny<CancellationToken>()))
            .Callback<SavedPayoffPlan, CancellationToken>((p, _) =>
            {
                p.SavedPayoffPlanId = 42;
                added = p;
            })
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService(repo.Object);
        var result = await sut.CreateSavedPayoffPlanAsync(new SavePayoffPlanRequest
        {
            Name = "Avalanche · Extra $100 · 7/21/26 8:49 AM",
            Strategy = "Avalanche",
            ExtraMonthlyPayment = 100m,
            TotalMonthlyDebtPayment = 500m,
            TargetUtilizationPercent = 30m,
            PayOverLimitFirst = true,
            PostUtilizationStrategy = "Snowball",
            EnableCashAdvanceBalanceMoves = true,
            PromotionalTransfers =
            [
                new PromotionalBalanceTransferDto
                {
                    FromCreditCardId = 1,
                    ToCreditCardId = 2,
                    PromotionalAnnualPercentageRate = 0m,
                    PromotionalPeriodMonths = 12,
                    ApplyAtMonthOffset = 0
                }
            ]
        });

        result.Success.Should().BeTrue();
        result.Data!.SavedPayoffPlanId.Should().Be(42);
        result.Data.EnableCashAdvanceBalanceMoves.Should().BeTrue();
        result.Data.PostUtilizationStrategy.Should().Be("Snowball");
        result.Data.PromotionalTransfers.Should().HaveCount(1);
        added.Should().NotBeNull();
        added!.PromotionalTransfersJson.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CompareSavedPayoffPlansRequestValidator_RejectsMoreThanThreeIds()
    {
        var validator = new CompareSavedPayoffPlansRequestValidator();
        var result = validator.Validate(new CompareSavedPayoffPlansRequest
        {
            PlanIds = [1, 2, 3, 4]
        });
        result.IsValid.Should().BeFalse();
    }

    private static CreditCardDecisionService CreateService(ISavedPayoffPlanRepository repo)
    {
        var payoff = new PayoffStrategyEngine();
        return new CreditCardDecisionService(
            Mock.Of<IAccountRepository>(),
            Mock.Of<IForecastScenarioRepository>(),
            repo,
            Mock.Of<IActivePayoffPlanRepository>(),
            new InterestCalculationEngine(),
            new UtilizationEngine(),
            payoff,
            new BalanceTransferEngine(),
            new CashFlowEngine(),
            new ForecastEngine(payoff));
    }
}
