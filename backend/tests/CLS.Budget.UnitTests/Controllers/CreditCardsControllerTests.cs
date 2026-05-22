using CLS.Budget.Api.Controllers;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CLS.Budget.UnitTests.Controllers;

public class CreditCardsControllerTests
{
    private readonly Mock<ICreditCardService> _service = new();
    private readonly CreditCardsController _sut;

    public CreditCardsControllerTests()
    {
        _sut = new CreditCardsController(_service.Object);
        ControllerTestHelper.AttachControllerContext(_sut, "CreditCards");
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCreditCards()
    {
        var cards = new List<AccountResponse> { SampleCreditCard() };
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IReadOnlyList<AccountResponse>>.Ok(cards));

        var result = await _sut.GetAll(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.GetEnvelope<IReadOnlyList<AccountResponse>>().Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenCreditCardMissing()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Fail("Credit card 99 was not found."));

        var result = await _sut.GetById(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreateAccountRequest
        {
            Name = "Visa",
            Number = "CC-001",
            AccountCategoryId = 1
        };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AccountResponse>.Ok(SampleCreditCard()));

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(CreditCardsController.GetById));
        created.RouteValues!["id"].Should().Be(1);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenCreditCardDeleted()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<object>.Ok(new object()));

        var result = await _sut.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    private static AccountResponse SampleCreditCard() => new()
    {
        AccountId = 1,
        Name = "Visa",
        Number = "CC-001",
        Balance = 500m,
        Limit = 5000m,
        AccountOpenDate = DateTime.UtcNow,
        Phone = "555-0100",
        Email = "test@example.com",
        Url = "https://bank.example.com",
        AccountCategoryId = 1
    };
}
