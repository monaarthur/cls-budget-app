using CLS.Budget.Api.Controllers;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.Incomes.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CLS.Budget.UnitTests.Controllers;

public class IncomesControllerTests
{
    private readonly Mock<IBudgetIncomeService> _service = new();
    private readonly IncomesController _sut;

    public IncomesControllerTests()
    {
        _sut = new IncomesController(_service.Object);
        ControllerTestHelper.AttachControllerContext(_sut, "Incomes");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenIncomeExists()
    {
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IncomeResponse>.Ok(SampleIncome()));

        var result = await _sut.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.GetEnvelope<IncomeResponse>().Data!.BudgetIncomeId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIncomeMissing()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IncomeResponse>.Fail("Income with id 99 was not found."));

        var result = await _sut.GetById(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenSuccessful()
    {
        var request = new CreateIncomeRequest
        {
            BudgetId = 1,
            IncomeSourceId = 2,
            Amount = 1500m,
            ReceivedDate = DateTime.UtcNow
        };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IncomeResponse>.Ok(SampleIncome()));

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(IncomesController.GetById));
        created.RouteValues!["id"].Should().Be(1);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceFails()
    {
        var request = new CreateIncomeRequest { BudgetId = 99, IncomeSourceId = 2, Amount = 10m, ReceivedDate = DateTime.UtcNow };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IncomeResponse>.Fail("Budget with id 99 was not found."));

        var result = await _sut.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSummary_ReturnsNotFound_WhenBudgetMissing()
    {
        _service.Setup(s => s.GetSummaryByBudgetIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IncomeSummaryResponse>.Fail("Budget with id 42 was not found."));

        var result = await _sut.GetSummary(42, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        _service.Setup(s => s.DeleteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<object>.Ok(new { }));

        var result = await _sut.Delete(3, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    private static IncomeResponse SampleIncome() => new()
    {
        BudgetIncomeId = 1,
        BudgetId = 1,
        IncomeSourceId = 2,
        IncomeSourceName = "Credit Cards",
        Amount = 1500m,
        ReceivedDate = DateTime.UtcNow
    };
}
