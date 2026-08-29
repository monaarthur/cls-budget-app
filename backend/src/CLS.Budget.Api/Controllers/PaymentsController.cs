using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.Payments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await paymentService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.BudgetPaymentId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("budget/{budgetId:int}/statuses")]
    [ProducesResponseType(typeof(ApiResponse<ResetBudgetPaymentStatusesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResetBudgetPaymentStatusesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ResetBudgetPaymentStatusesResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetBudgetStatuses(
        int budgetId,
        [FromBody] ResetBudgetPaymentStatusesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ResetBudgetStatusesAsync(
            budgetId,
            request,
            cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("was not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOwner)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await paymentService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : NotFound(result);
    }
}
