using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Incomes.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/incomes")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class IncomesController(IBudgetIncomeService incomeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await incomeService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await incomeService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("budget/{budgetId:int}")]
    public async Task<IActionResult> GetByBudget(int budgetId, CancellationToken cancellationToken)
    {
        var result = await incomeService.GetByBudgetIdAsync(budgetId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("budget/{budgetId:int}/summary")]
    public async Task<IActionResult> GetSummary(int budgetId, CancellationToken cancellationToken)
    {
        var result = await incomeService.GetSummaryByBudgetIdAsync(budgetId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await incomeService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.BudgetIncomeId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await incomeService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOwner)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await incomeService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : NotFound(result);
    }
}
