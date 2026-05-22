using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Accounts;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CLS.Budget.UnitTests.Accounts;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _repository = new();
    private readonly AccountService _sut;

    public AccountServiceTests() => _sut = new AccountService(_repository.Object);

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenAccountMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("99"));
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedAccount()
    {
        var request = new CreateAccountRequest
        {
            Name = "Checking",
            Number = "CHK-001",
            Balance = 100m,
            Limit = 0m,
            AccountOpenDate = DateTime.UtcNow,
            MonthlyPayment = 50m,
            Phone = "555-0100",
            Email = "test@example.com",
            Url = "https://bank.example.com",
            AccountCategoryId = 1
        };

        _repository.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account account, CancellationToken _) =>
            {
                account.AccountId = 1;
                return account;
            });

        var result = await _sut.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.AccountId.Should().Be(1);
        result.Data.Name.Should().Be("Checking");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_WhenAccountMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _sut.DeleteAsync(5);

        result.Success.Should().BeFalse();
    }
}
