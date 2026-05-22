using CLS.Budget.Api.Controllers;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.Payments.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CLS.Budget.UnitTests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _service = new();
    private readonly PaymentsController _sut;

    public PaymentsControllerTests()
    {
        _sut = new PaymentsController(_service.Object);
        ControllerTestHelper.AttachControllerContext(_sut, "Payments");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenPaymentExists()
    {
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<PaymentResponse>.Ok(SamplePayment()));

        var result = await _sut.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.GetEnvelope<PaymentResponse>().Data!.BudgetPaymentId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenPaymentMissing()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<PaymentResponse>.Fail("Payment 99 was not found."));

        var result = await _sut.GetById(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreatePaymentRequest
        {
            BudgetId = 1,
            AccountId = 1,
            PaymentMade = 50m,
            Amount = 50m,
            BudgetPaymentStatusId = 1,
            PaymentDate = DateTime.UtcNow
        };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<PaymentResponse>.Ok(SamplePayment()));

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(PaymentsController.GetById));
        created.RouteValues!["id"].Should().Be(1);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenPaymentMissing()
    {
        _service.Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<object>.Fail("Payment 5 was not found."));

        var result = await _sut.Delete(5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static PaymentResponse SamplePayment() => new()
    {
        BudgetPaymentId = 1,
        BudgetId = 1,
        AccountId = 1,
        PaymentMade = 50m,
        Amount = 50m,
        BudgetPaymentStatusId = 1,
        BudgetPaymentStatusName = "Pending",
        PaymentDate = DateTime.UtcNow
    };
}
