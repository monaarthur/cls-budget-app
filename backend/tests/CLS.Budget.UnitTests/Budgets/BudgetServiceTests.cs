using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Budgets;
using CLS.Budget.Application.Budgets.Dtos;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.Budgets;

public class BudgetServiceTests
{
    private readonly Mock<IBudgetRepository> _budgetRepository = new();
    private readonly Mock<IBudgetTemplateRepository> _templateRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly BudgetService _sut;

    public BudgetServiceTests() =>
        _sut = new BudgetService(_budgetRepository.Object, _templateRepository.Object, _accountRepository.Object);

    [Fact]
    public async Task CreateAsync_CreatesBudgetPayment_ForEachTemplateAccountId()
    {
        var request = new CreateBudgetRequest
        {
            Name = "May 2026",
            StartPeriod = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndPeriod = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            BudgetTemplateId = 1
        };

        _templateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetTemplate
            {
                BudgetTemplateId = 1,
                Name = "Default",
                AccountIds = "[1,2]"
            });

        _accountRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<int> ids, CancellationToken _) =>
            [
                new Account
                {
                    AccountId = 1,
                    Name = "A",
                    Number = "1",
                    MonthlyPayment = 100m,
                    AccountOpenDate = DateTime.UtcNow,
                    Phone = "p",
                    Email = "e",
                    Url = "u",
                    AccountCategoryId = 1
                },
                new Account
                {
                    AccountId = 2,
                    Name = "B",
                    Number = "2",
                    MonthlyPayment = 50m,
                    AccountOpenDate = DateTime.UtcNow,
                    Phone = "p",
                    Email = "e",
                    Url = "u",
                    AccountCategoryId = 1
                }
            ]);

        _budgetRepository
            .Setup(r => r.AddWithPaymentsForAccountsAsync(
                It.Is<BudgetModel>(b => b.AccountIds == "[1,2]"),
                It.Is<IReadOnlyList<Account>>(a => a.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel b, IReadOnlyList<Account> _, CancellationToken _) =>
            {
                b.BudgetId = 10;
                return b;
            });

        var result = await _sut.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.BudgetId.Should().Be(10);
        result.Data.AccountIds.Should().Equal(1, 2);
        _budgetRepository.Verify(
            r => r.AddWithPaymentsForAccountsAsync(
                It.Is<BudgetModel>(b => b.AccountIds == "[1,2]"),
                It.Is<IReadOnlyList<Account>>(a => a[0].AccountId == 1 && a[1].AccountId == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SkipsDeletedAccounts_WhenTemplateReferencesMissingIds()
    {
        _templateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetTemplate
            {
                BudgetTemplateId = 1,
                Name = "Default",
                AccountIds = "[1,19,2]"
            });

        _accountRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<int> ids, CancellationToken _) =>
            [
                new Account
                {
                    AccountId = 1,
                    Name = "A",
                    Number = "1",
                    MonthlyPayment = 100m,
                    AccountOpenDate = DateTime.UtcNow,
                    Phone = "p",
                    Email = "e",
                    Url = "u",
                    AccountCategoryId = 1
                },
                new Account
                {
                    AccountId = 2,
                    Name = "B",
                    Number = "2",
                    MonthlyPayment = 50m,
                    AccountOpenDate = DateTime.UtcNow,
                    Phone = "p",
                    Email = "e",
                    Url = "u",
                    AccountCategoryId = 1
                }
            ]);

        _budgetRepository
            .Setup(r => r.AddWithPaymentsForAccountsAsync(
                It.Is<BudgetModel>(b => b.AccountIds == "[1,2]"),
                It.Is<IReadOnlyList<Account>>(a => a.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel b, IReadOnlyList<Account> _, CancellationToken _) =>
            {
                b.BudgetId = 12;
                return b;
            });

        var result = await _sut.CreateAsync(new CreateBudgetRequest
        {
            Name = "July 2026",
            StartPeriod = DateTime.UtcNow,
            EndPeriod = DateTime.UtcNow,
            BudgetTemplateId = 1
        });

        result.Success.Should().BeTrue();
        result.Data!.AccountIds.Should().Equal(1, 2);
    }

    [Fact]
    public async Task CreateAsync_UsesAllAccounts_WhenTemplateHasNoAccountIds()
    {
        _templateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetTemplate { BudgetTemplateId = 1, Name = "Empty", AccountIds = null });

        _accountRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken _) =>
            [
                new Account
                {
                    AccountId = 1,
                    Name = "A",
                    Number = "1",
                    MonthlyPayment = 100m,
                    AccountOpenDate = DateTime.UtcNow,
                    Phone = "p",
                    Email = "e",
                    Url = "u",
                    AccountCategoryId = 1
                }
            ]);

        _budgetRepository
            .Setup(r => r.AddWithPaymentsForAccountsAsync(
                It.IsAny<BudgetModel>(),
                It.IsAny<IReadOnlyList<Account>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel b, IReadOnlyList<Account> _, CancellationToken _) =>
            {
                b.BudgetId = 11;
                return b;
            });

        var result = await _sut.CreateAsync(new CreateBudgetRequest
        {
            Name = "June 2026",
            StartPeriod = DateTime.UtcNow,
            EndPeriod = DateTime.UtcNow,
            BudgetTemplateId = 1
        });

        result.Success.Should().BeTrue();
        result.Data!.BudgetId.Should().Be(11);
        _accountRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CopyAsync_CopiesBudgetAndPayments_FromSource()
    {
        var source = new BudgetModel
        {
            BudgetId = 1,
            Name = "May 2026",
            StartPeriod = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndPeriod = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            BudgetTemplateId = 1,
            AccountIds = "[1,2]",
            BudgetPayments =
            [
                new BudgetPayment
                {
                    BudgetPaymentId = 10,
                    AccountId = 1,
                    PaymentMade = 100m,
                    Amount = 100m,
                    BudgetPaymentStatusId = 3,
                    IsCleared = true,
                    PaymentDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    PaymentSourceId = 2
                },
                new BudgetPayment
                {
                    BudgetPaymentId = 11,
                    AccountId = 2,
                    PaymentMade = 50m,
                    Amount = 50m,
                    BudgetPaymentStatusId = 1,
                    IsCleared = false,
                    PaymentDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var request = new CopyBudgetRequest
        {
            Name = "June 2026",
            StartPeriod = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndPeriod = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
        };

        _budgetRepository.Setup(r => r.GetByIdWithPaymentsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        _accountRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<int> ids, CancellationToken _) =>
                ids.Select(id => new Account
                {
                    AccountId = id,
                    Name = $"Account {id}",
                    Number = id.ToString(),
                    MonthlyPayment = 10m,
                    AccountOpenDate = DateTime.UtcNow,
                    Phone = "p",
                    Email = "e",
                    Url = "u",
                    AccountCategoryId = 1
                }).ToList());

        _budgetRepository
            .Setup(r => r.CopyWithPaymentsAsync(
                source,
                It.Is<BudgetModel>(b =>
                    b.Name == request.Name
                    && b.StartPeriod == request.StartPeriod
                    && b.EndPeriod == request.EndPeriod
                    && b.BudgetTemplateId == source.BudgetTemplateId
                    && b.AccountIds == source.AccountIds),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel _, BudgetModel b, CancellationToken _) =>
            {
                b.BudgetId = 20;
                return b;
            });

        var result = await _sut.CopyAsync(1, request);

        result.Success.Should().BeTrue();
        result.Data!.BudgetId.Should().Be(20);
        result.Data.Name.Should().Be("June 2026");
        result.Data.AccountIds.Should().Equal(1, 2);
        _budgetRepository.Verify(
            r => r.CopyWithPaymentsAsync(source, It.IsAny<BudgetModel>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CopyAsync_ReturnsFailure_WhenSourceBudgetNotFound()
    {
        _budgetRepository.Setup(r => r.GetByIdWithPaymentsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BudgetModel?)null);

        var result = await _sut.CopyAsync(99, new CopyBudgetRequest
        {
            Name = "Copy",
            StartPeriod = DateTime.UtcNow,
            EndPeriod = DateTime.UtcNow
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("99", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddAccountAsync_AddsAccountAndPayment_WhenValid()
    {
        var budget = new BudgetModel
        {
            BudgetId = 1,
            Name = "May 2026",
            StartPeriod = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndPeriod = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            BudgetTemplateId = 1,
            AccountIds = "[1]"
        };

        var account = new Account
        {
            AccountId = 2,
            Name = "B",
            Number = "2",
            MonthlyPayment = 50m,
            AccountOpenDate = DateTime.UtcNow,
            Phone = "p",
            Email = "e",
            Url = "u",
            AccountCategoryId = 1
        };

        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(budget);
        _accountRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _budgetRepository
            .Setup(r => r.AddAccountWithPaymentAsync(
                It.Is<BudgetModel>(b => b.AccountIds == "[1,2]"),
                account,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.AddAccountAsync(1, 2);

        result.Success.Should().BeTrue();
        result.Data!.AccountIds.Should().Equal(1, 2);
        _budgetRepository.Verify(
            r => r.AddAccountWithPaymentAsync(
                It.Is<BudgetModel>(b => b.AccountIds == "[1,2]"),
                account,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAccountAsync_ReturnsFailure_WhenAccountAlreadyInBudget()
    {
        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetModel
            {
                BudgetId = 1,
                Name = "May 2026",
                StartPeriod = DateTime.UtcNow,
                EndPeriod = DateTime.UtcNow,
                BudgetTemplateId = 1,
                AccountIds = "[1,2]"
            });

        var result = await _sut.AddAccountAsync(1, 2);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already included", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RemoveAccountAsync_RemovesAccountAndPayment_WhenValid()
    {
        var budget = new BudgetModel
        {
            BudgetId = 1,
            Name = "May 2026",
            StartPeriod = DateTime.UtcNow,
            EndPeriod = DateTime.UtcNow,
            BudgetTemplateId = 1,
            AccountIds = "[1,2]"
        };

        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(budget);
        _budgetRepository
            .Setup(r => r.RemoveAccountWithPaymentAsync(
                It.Is<BudgetModel>(b => b.AccountIds == "[1]"),
                2,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RemoveAccountAsync(1, 2);

        result.Success.Should().BeTrue();
        result.Data!.AccountIds.Should().Equal(1);
    }

    [Fact]
    public async Task RemoveAccountAsync_ReturnsFailure_WhenRemovingLastAccount()
    {
        _budgetRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetModel
            {
                BudgetId = 1,
                Name = "May 2026",
                StartPeriod = DateTime.UtcNow,
                EndPeriod = DateTime.UtcNow,
                BudgetTemplateId = 1,
                AccountIds = "[1]"
            });

        var result = await _sut.RemoveAccountAsync(1, 1);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("last account", StringComparison.OrdinalIgnoreCase));
    }
}
