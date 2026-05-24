using CLS.Budget.Api.Controllers;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Budgets.Dtos;
using CLS.Budget.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CLS.Budget.UnitTests.Controllers;

public class BudgetsControllerTests
{
    private readonly Mock<IBudgetService> _service = new();
    private readonly BudgetsController _sut;

    public BudgetsControllerTests()
    {
        _sut = new BudgetsController(_service.Object);
        ControllerTestHelper.AttachControllerContext(_sut, "Budgets");
    }

    [Fact]
    public async Task GetByMonthAndYear_ReturnsOk_WhenBudgetsFound()
    {
        var budgets = new List<BudgetResponse> { SampleBudget() };
        _service.Setup(s => s.GetByMonthAndYearAsync(5, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IReadOnlyList<BudgetResponse>>.Ok(budgets));

        var result = await _sut.GetByMonthAndYear(5, 2026, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        result.GetEnvelope<IReadOnlyList<BudgetResponse>>().Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByMonthAndYear_ReturnsBadRequest_WhenServiceFails()
    {
        _service.Setup(s => s.GetByMonthAndYearAsync(13, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<IReadOnlyList<BudgetResponse>>.Fail("Month must be between 1 and 12."));

        var result = await _sut.GetByMonthAndYear(13, 2026, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        result.GetEnvelope<IReadOnlyList<BudgetResponse>>().Errors.Should().Contain("Month must be between 1 and 12.");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenBudgetMissing()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Fail("Budget 99 was not found."));

        var result = await _sut.GetById(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreateBudgetRequest
        {
            Name = "May 2026",
            StartPeriod = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndPeriod = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            BudgetTemplateId = 1
        };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Ok(SampleBudget()));

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BudgetsController.GetById));
        created.RouteValues!["id"].Should().Be(1);
    }

    [Fact]
    public async Task Copy_ReturnsCreatedAtAction_WhenCopySucceeds()
    {
        var request = new CopyBudgetRequest
        {
            Name = "June 2026",
            StartPeriod = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndPeriod = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
        };

        _service.Setup(s => s.CopyAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Ok(new BudgetResponse
            {
                BudgetId = 2,
                Name = request.Name,
                StartPeriod = request.StartPeriod,
                EndPeriod = request.EndPeriod,
                BudgetTemplateId = 1,
                AccountIds = [1, 2]
            }));

        var result = await _sut.Copy(1, request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BudgetsController.GetById));
        created.RouteValues!["id"].Should().Be(2);
    }

    [Fact]
    public async Task Copy_ReturnsNotFound_WhenSourceBudgetMissing()
    {
        var request = new CopyBudgetRequest
        {
            Name = "Copy",
            StartPeriod = DateTime.UtcNow,
            EndPeriod = DateTime.UtcNow
        };

        _service.Setup(s => s.CopyAsync(99, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Fail("Budget with id 99 was not found."));

        var result = await _sut.Copy(99, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenBudgetDeleted()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<object>.Ok(new object()));

        var result = await _sut.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddAccount_ReturnsOk_WhenAccountAdded()
    {
        _service.Setup(s => s.AddAccountAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Ok(new BudgetResponse
            {
                BudgetId = 1,
                Name = "May 2026",
                StartPeriod = DateTime.UtcNow,
                EndPeriod = DateTime.UtcNow,
                BudgetTemplateId = 1,
                AccountIds = [1, 2]
            }));

        var result = await _sut.AddAccount(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddAccount_ReturnsBadRequest_WhenAccountAlreadyIncluded()
    {
        _service.Setup(s => s.AddAccountAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Fail("Account with id 2 is already included in budget 1."));

        var result = await _sut.AddAccount(1, 2, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveAccount_ReturnsOk_WhenAccountRemoved()
    {
        _service.Setup(s => s.RemoveAccountAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<BudgetResponse>.Ok(new BudgetResponse
            {
                BudgetId = 1,
                Name = "May 2026",
                StartPeriod = DateTime.UtcNow,
                EndPeriod = DateTime.UtcNow,
                BudgetTemplateId = 1,
                AccountIds = [1]
            }));

        var result = await _sut.RemoveAccount(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    private static BudgetResponse SampleBudget() => new()
    {
        BudgetId = 1,
        Name = "May 2026",
        StartPeriod = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        EndPeriod = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
        BudgetTemplateId = 1
    };
}
