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

public sealed class ActivePayoffPlanServiceTests
{
    [Fact]
    public void ApplyPaymentToAccount_ReducesBalanceAndMarksPaidOffAtZero()
    {
        var account = new Account
        {
            AccountId = 1,
            Balance = 100m,
            IsPaidOff = false
        };

        var applied = CreditCardDecisionService.ApplyPaymentToAccount(account, 40m);
        applied.Should().Be(40m);
        account.Balance.Should().Be(60m);
        account.IsPaidOff.Should().BeFalse();

        CreditCardDecisionService.ApplyPaymentToAccount(account, 100m);
        account.Balance.Should().Be(0m);
        account.IsPaidOff.Should().BeTrue();
        account.PaidOffDate.Should().NotBeNull();
    }

    [Fact]
    public void RestorePaymentToAccount_ReopensPaidOffCard()
    {
        var account = new Account
        {
            AccountId = 1,
            Balance = 0m,
            IsPaidOff = true,
            PaidOffDate = DateTime.UtcNow.Date
        };

        CreditCardDecisionService.RestorePaymentToAccount(account, 50m);
        account.Balance.Should().Be(50m);
        account.IsPaidOff.Should().BeFalse();
        account.PaidOffDate.Should().BeNull();
    }

    [Fact]
    public async Task ActivatePayoffPlanAsync_ArchivesExistingActiveAndCreatesVersionOne()
    {
        var existing = new ActivePayoffPlan
        {
            ActivePayoffPlanId = 7,
            Name = "Old plan",
            Status = ActivePayoffPlanStatuses.Active,
            Strategy = "Avalanche",
            TotalMonthlyDebtPayment = 200m,
            CurrentVersionNumber = 1,
            StartedOnUtc = DateTime.UtcNow.AddDays(-10)
        };

        ActivePayoffPlan? added = null;
        var events = new List<PayoffPlanEvent>();
        var activeRepo = new Mock<IActivePayoffPlanRepository>();
        activeRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        activeRepo.Setup(r => r.AddAsync(It.IsAny<ActivePayoffPlan>(), It.IsAny<CancellationToken>()))
            .Callback<ActivePayoffPlan, CancellationToken>((p, _) =>
            {
                p.ActivePayoffPlanId = 99;
                added = p;
            })
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.AddEventAsync(It.IsAny<PayoffPlanEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PayoffPlanEvent, CancellationToken>((e, _) => events.Add(e))
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetByCategoryAsync(CreditCardCategory.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Account
                {
                    AccountId = 1,
                    Name = "Card A",
                    Balance = 500m,
                    Limit = 2000m,
                    IsPaidOff = false,
                    AccountCategoryId = CreditCardCategory.CategoryId,
                    MonthlyPayment = 50m,
                    CreditCardDetail = new CreditCardDetail
                    {
                        InterestRate = 22m,
                        IncludeInPayoffAnalysis = true
                    }
                }
            ]);

        var sut = CreateService(accounts.Object, activeRepo.Object);
        var result = await sut.ActivatePayoffPlanAsync(new ActivatePayoffPlanRequest
        {
            Name = "New plan",
            Strategy = "Snowball",
            ExtraMonthlyPayment = 25m,
            TotalMonthlyDebtPayment = 250m
        });

        result.Success.Should().BeTrue();
        existing.Status.Should().Be(ActivePayoffPlanStatuses.Completed);
        existing.EndedOnUtc.Should().NotBeNull();
        events.Should().Contain(e => e.EventType == PayoffPlanEventTypes.Completed);
        added.Should().NotBeNull();
        added!.Status.Should().Be(ActivePayoffPlanStatuses.Active);
        added.CurrentVersionNumber.Should().Be(1);
        added.Versions.Should().HaveCount(1);
        added.Versions.First().VersionNumber.Should().Be(1);
        added.StartingDebt.Should().Be(500m);
        added.Events.Should().Contain(e => e.EventType == PayoffPlanEventTypes.Started);
        result.Data!.Progress.StartingDebt.Should().Be(500m);
    }

    [Fact]
    public async Task RecordActivePayoffPlanPaymentAsync_UpdatesBalanceAndWritesEvent()
    {
        var version = new PayoffPlanVersion
        {
            PayoffPlanVersionId = 3,
            VersionNumber = 1,
            Strategy = "Avalanche",
            TotalMonthlyDebtPayment = 200m
        };
        var plan = new ActivePayoffPlan
        {
            ActivePayoffPlanId = 11,
            Name = "Live",
            Status = ActivePayoffPlanStatuses.Active,
            Strategy = "Avalanche",
            TotalMonthlyDebtPayment = 200m,
            CurrentVersionNumber = 1,
            StartingDebt = 500m,
            StartedOnUtc = DateTime.UtcNow.AddDays(-5),
            Versions = [version],
            Payments = [],
            Events = []
        };

        var account = new Account
        {
            AccountId = 5,
            Name = "Chase",
            Balance = 400m,
            Limit = 1000m,
            AccountCategoryId = CreditCardCategory.CategoryId,
            IsPaidOff = false
        };

        PayoffPlanPayment? payment = null;
        PayoffPlanEvent? planEvent = null;
        var activeRepo = new Mock<IActivePayoffPlanRepository>();
        activeRepo.Setup(r => r.GetActiveWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        activeRepo.Setup(r => r.AddPaymentAsync(It.IsAny<PayoffPlanPayment>(), It.IsAny<CancellationToken>()))
            .Callback<PayoffPlanPayment, CancellationToken>((p, _) =>
            {
                p.PayoffPlanPaymentId = 44;
                payment = p;
            })
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.AddEventAsync(It.IsAny<PayoffPlanEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PayoffPlanEvent, CancellationToken>((e, _) => planEvent = e)
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var sut = CreateService(accounts.Object, activeRepo.Object);
        var result = await sut.RecordActivePayoffPlanPaymentAsync(new RecordPayoffPlanPaymentRequest
        {
            AccountId = 5,
            Amount = 150m
        });

        result.Success.Should().BeTrue();
        account.Balance.Should().Be(250m);
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(150m);
        payment.PayoffPlanVersionId.Should().Be(3);
        planEvent!.EventType.Should().Be(PayoffPlanEventTypes.PaymentRecorded);
    }

    [Fact]
    public async Task ReviseActivePayoffPlanAsync_CreatesNewVersion()
    {
        var plan = new ActivePayoffPlan
        {
            ActivePayoffPlanId = 11,
            Name = "Live",
            Status = ActivePayoffPlanStatuses.Active,
            Strategy = "Avalanche",
            TotalMonthlyDebtPayment = 200m,
            ExtraMonthlyPayment = 0m,
            CurrentVersionNumber = 1,
            StartingDebt = 500m,
            StartedOnUtc = DateTime.UtcNow.AddDays(-5),
            Versions =
            [
                new PayoffPlanVersion
                {
                    PayoffPlanVersionId = 1,
                    VersionNumber = 1,
                    Strategy = "Avalanche",
                    TotalMonthlyDebtPayment = 200m
                }
            ],
            Payments = [],
            Events = []
        };

        PayoffPlanVersion? newVersion = null;
        var activeRepo = new Mock<IActivePayoffPlanRepository>();
        activeRepo.Setup(r => r.GetActiveWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        activeRepo.Setup(r => r.AddVersionAsync(It.IsAny<PayoffPlanVersion>(), It.IsAny<CancellationToken>()))
            .Callback<PayoffPlanVersion, CancellationToken>((v, _) => newVersion = v)
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.AddEventAsync(It.IsAny<PayoffPlanEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetByCategoryAsync(CreditCardCategory.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Account
                {
                    AccountId = 1,
                    Name = "Card A",
                    Balance = 400m,
                    Limit = 2000m,
                    IsPaidOff = false,
                    AccountCategoryId = CreditCardCategory.CategoryId,
                    MonthlyPayment = 50m,
                    CreditCardDetail = new CreditCardDetail
                    {
                        InterestRate = 22m,
                        IncludeInPayoffAnalysis = true
                    }
                }
            ]);

        var sut = CreateService(accounts.Object, activeRepo.Object);
        var result = await sut.ReviseActivePayoffPlanAsync(new ReviseActivePayoffPlanRequest
        {
            Name = "Live revised",
            Strategy = "Snowball",
            ExtraMonthlyPayment = 50m,
            TotalMonthlyDebtPayment = 300m,
            Reason = "Raise"
        });

        result.Success.Should().BeTrue();
        plan.CurrentVersionNumber.Should().Be(2);
        plan.Strategy.Should().Be("Snowball");
        newVersion.Should().NotBeNull();
        newVersion!.VersionNumber.Should().Be(2);
        newVersion.Reason.Should().Be("Raise");
    }

    [Fact]
    public async Task VoidActivePayoffPlanPaymentAsync_RestoresBalance()
    {
        var plan = new ActivePayoffPlan
        {
            ActivePayoffPlanId = 11,
            Status = ActivePayoffPlanStatuses.Active,
            Strategy = "Avalanche",
            TotalMonthlyDebtPayment = 200m,
            StartedOnUtc = DateTime.UtcNow
        };
        var payment = new PayoffPlanPayment
        {
            PayoffPlanPaymentId = 9,
            ActivePayoffPlanId = 11,
            AccountId = 5,
            Amount = 75m,
            IsVoided = false
        };
        var account = new Account
        {
            AccountId = 5,
            Name = "Chase",
            Balance = 325m,
            AccountCategoryId = CreditCardCategory.CategoryId
        };

        var activeRepo = new Mock<IActivePayoffPlanRepository>();
        activeRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        activeRepo.Setup(r => r.GetPaymentAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        activeRepo.Setup(r => r.AddEventAsync(It.IsAny<PayoffPlanEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        activeRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var sut = CreateService(accounts.Object, activeRepo.Object);
        var result = await sut.VoidActivePayoffPlanPaymentAsync(9);

        result.Success.Should().BeTrue();
        payment.IsVoided.Should().BeTrue();
        account.Balance.Should().Be(400m);
    }

    private static CreditCardDecisionService CreateService(
        IAccountRepository accounts,
        IActivePayoffPlanRepository activeRepo)
    {
        var payoff = new PayoffStrategyEngine();
        return new CreditCardDecisionService(
            accounts,
            Mock.Of<IForecastScenarioRepository>(),
            Mock.Of<ISavedPayoffPlanRepository>(),
            activeRepo,
            new InterestCalculationEngine(),
            new UtilizationEngine(),
            payoff,
            new BalanceTransferEngine(),
            new CashFlowEngine(),
            new ForecastEngine(payoff));
    }
}
