using CLS.Budget.Api.Controllers;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CLS.Budget.UnitTests.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IAccountService> _service = new();
    private readonly AccountsController _sut;

    public AccountsControllerTests()
    {
        _sut = new AccountsController(_service.Object);
        ControllerTestHelper.AttachControllerContext(_sut, "Accounts");
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithAccounts()
    {
        var accounts = new List<AccountResponse> { SampleAccount() };
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IReadOnlyList<AccountResponse>>.Ok(accounts));

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value.Should().BeOfType<ApiResponse<IReadOnlyList<AccountResponse>>>().Subject;
        envelope.Success.Should().BeTrue();
        envelope.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenAccountExists()
    {
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Ok(SampleAccount()));

        var result = await _sut.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.GetEnvelope<AccountResponse>().Data!.AccountId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenAccountMissing()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Fail("Account 99 was not found."));

        var result = await _sut.GetById(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        result.GetEnvelope<AccountResponse>().Success.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreateAccountRequest { Name = "Checking", Number = "CHK-1", AccountCategoryId = 1 };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Ok(SampleAccount()));

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(AccountsController.GetById));
        created.RouteValues!["id"].Should().Be(1);
        created.GetEnvelope<AccountResponse>().Data!.Name.Should().Be("Checking");
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenAccountExists()
    {
        var request = new UpdateAccountRequest { Name = "Updated" };
        _service.Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Ok(SampleAccount(1, "Updated")));

        var result = await _sut.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.GetEnvelope<AccountResponse>().Data!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenAccountMissing()
    {
        var request = new UpdateAccountRequest { Name = "Updated" };
        _service.Setup(s => s.UpdateAsync(5, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Fail("Account 5 was not found."));

        var result = await _sut.Update(5, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenAccountDeleted()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<object>.Ok(new object()));

        var result = await _sut.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenAccountMissing()
    {
        _service.Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<object>.Fail("Account 5 was not found."));

        var result = await _sut.Delete(5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static AccountResponse SampleAccount(int id = 1, string name = "Checking") => new()
    {
        AccountId = id,
        Name = name,
        Number = "CHK-001",
        Balance = 100m,
        Limit = 0m,
        AccountOpenDate = DateTime.UtcNow,
        Phone = "555-0100",
        Email = "test@example.com",
        Url = "https://bank.example.com",
        AccountCategoryId = 1
    };
}
