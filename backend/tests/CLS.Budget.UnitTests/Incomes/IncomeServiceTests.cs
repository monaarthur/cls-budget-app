using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Incomes;
using CLS.Budget.Application.Incomes.Dtos;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.Incomes;

public class IncomeServiceTests
{
    private readonly Mock<IBudgetIncomeRepository> _incomeRepository = new();
    private readonly Mock<IBudgetRepository> _budgetRepository = new();
    private readonly Mock<IIncomeSourceRepository> _incomeSourceRepository = new();
    private readonly IncomeService _sut;

    public IncomeServiceTests() =>
        _sut = new IncomeService(
            _incomeRepository.Object,
            _budgetRepository.Object,
            _incomeSourceRepository.Object);

    [Fact]
    public async Task CreateAsync_ReturnsCreated_WhenBudgetAndSourceValid()
    {
        var request = new CreateIncomeRequest
        {
            BudgetId = 1,
            IncomeSourceId = 2,
            Amount = 1500m,
            ReceivedDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleBudget());
        _incomeSourceRepository.Setup(r => r.ExistsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _incomeRepository.Setup(r => r.AddAsync(It.IsAny<BudgetIncome>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetIncome income, CancellationToken _) =>
            {
                income.BudgetIncomeId = 10;
                income.IncomeSource = new IncomeSource { IncomeSourceId = 2, Name = "Credit Cards", IsActive = true };
                return income;
            });

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.BudgetIncomeId.Should().Be(10);
        result.Data.IncomeSourceName.Should().Be("Credit Cards");
        _incomeRepository.Verify(r => r.AddAsync(It.IsAny<BudgetIncome>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenBudgetNotFound()
    {
        var request = new CreateIncomeRequest { BudgetId = 99, IncomeSourceId = 2, Amount = 100m, ReceivedDate = DateTime.UtcNow };

        _budgetRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel?)null);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Budget with id 99");
        _incomeRepository.Verify(r => r.AddAsync(It.IsAny<BudgetIncome>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenIncomeSourceNotFound()
    {
        var request = new CreateIncomeRequest { BudgetId = 1, IncomeSourceId = 77, Amount = 100m, ReceivedDate = DateTime.UtcNow };

        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleBudget());
        _incomeSourceRepository.Setup(r => r.ExistsAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Income source with id 77");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenIncomeMissing()
    {
        _incomeRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetIncome?)null);

        var result = await _sut.GetByIdAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Income with id 5");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenIncomeMissing()
    {
        _incomeRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetIncome?)null);

        var result = await _sut.UpdateAsync(
            5,
            new UpdateIncomeRequest { BudgetId = 1, IncomeSourceId = 2, Amount = 10m, ReceivedDate = DateTime.UtcNow },
            CancellationToken.None);

        result.Success.Should().BeFalse();
        _incomeRepository.Verify(r => r.UpdateAsync(It.IsAny<BudgetIncome>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesIncome_WhenFound()
    {
        var income = new BudgetIncome { BudgetIncomeId = 3, BudgetId = 1, IncomeSourceId = 1, Amount = 10m };
        _incomeRepository.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);

        var result = await _sut.DeleteAsync(3, CancellationToken.None);

        result.Success.Should().BeTrue();
        _incomeRepository.Verify(r => r.DeleteAsync(income, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSummaryByBudgetIdAsync_GroupsTotalsPerSource()
    {
        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleBudget());
        _incomeRepository.Setup(r => r.GetByBudgetIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetIncome>
            {
                new() { BudgetIncomeId = 1, BudgetId = 1, IncomeSourceId = 1, Amount = 1000m, IncomeSource = new IncomeSource { IncomeSourceId = 1, Name = "Job Income" } },
                new() { BudgetIncomeId = 2, BudgetId = 1, IncomeSourceId = 1, Amount = 500m, IncomeSource = new IncomeSource { IncomeSourceId = 1, Name = "Job Income" } },
                new() { BudgetIncomeId = 3, BudgetId = 1, IncomeSourceId = 2, Amount = 250m, IncomeSource = new IncomeSource { IncomeSourceId = 2, Name = "Credit Cards" } }
            });

        var result = await _sut.GetSummaryByBudgetIdAsync(1, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Total.Should().Be(1750m);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items.Single(i => i.IncomeSourceId == 1).Total.Should().Be(1500m);
        result.Data.Items.Single(i => i.IncomeSourceId == 2).Total.Should().Be(250m);
    }

    [Fact]
    public async Task GetSummaryByBudgetIdAsync_ReturnsFailure_WhenBudgetMissing()
    {
        _budgetRepository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel?)null);

        var result = await _sut.GetSummaryByBudgetIdAsync(42, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Budget with id 42");
    }

    private static BudgetModel SampleBudget() => new()
    {
        BudgetId = 1,
        Name = "May 2026",
        StartPeriod = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        EndPeriod = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
        BudgetTemplateId = 1
    };
}
