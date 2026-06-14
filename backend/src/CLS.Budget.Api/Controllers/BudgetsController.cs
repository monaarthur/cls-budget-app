using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Budgets.Dtos;
using CLS.Budget.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class BudgetsController(IBudgetService budgetService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await budgetService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("month/{month:int}/year/{year:int}")]
    public async Task<IActionResult> GetByMonthAndYear(int month, int year, CancellationToken cancellationToken)
    {
        var result = await budgetService.GetByMonthAndYearAsync(month, year, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await budgetService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var result = await budgetService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.BudgetId }, result);
    }

    [HttpPost("{id:int}/copy")]
    public async Task<IActionResult> Copy(
        int id,
        [FromBody] CopyBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await budgetService.CopyAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("was not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.BudgetId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var result = await budgetService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:int}/accounts/{accountId:int}")]
    public async Task<IActionResult> AddAccount(
        int id,
        int accountId,
        CancellationToken cancellationToken)
    {
        var result = await budgetService.AddAccountAsync(id, accountId, cancellationToken);
        if (!result.Success)
        {
            return MapAccountMutationFailure(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}/accounts/{accountId:int}")]
    public async Task<IActionResult> RemoveAccount(
        int id,
        int accountId,
        CancellationToken cancellationToken)
    {
        var result = await budgetService.RemoveAccountAsync(id, accountId, cancellationToken);
        if (!result.Success)
        {
            return MapAccountMutationFailure(result);
        }

        return Ok(result);
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOwner)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await budgetService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : NotFound(result);
    }

    private static IActionResult MapAccountMutationFailure(ApiResponse<BudgetResponse> result)
    {
        if (result.Errors.Any(e => e.Contains("was not found", StringComparison.OrdinalIgnoreCase)))
        {
            return new NotFoundObjectResult(result);
        }

        return new BadRequestObjectResult(result);
    }
}
