using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/payoff-plans")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public sealed class PayoffPlansController(ICreditCardDecisionService decisionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await decisionService.ListSavedPayoffPlansAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SavePayoffPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.CreateSavedPayoffPlanAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{savedPayoffPlanId:int}")]
    public async Task<IActionResult> Update(
        int savedPayoffPlanId,
        [FromBody] UpdateSavedPayoffPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.UpdateSavedPayoffPlanAsync(
            savedPayoffPlanId,
            request,
            cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{savedPayoffPlanId:int}")]
    public async Task<IActionResult> Delete(
        int savedPayoffPlanId,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.DeleteSavedPayoffPlanAsync(
            savedPayoffPlanId,
            cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("loan-savings")]
    public async Task<IActionResult> CompareLoanSavings(
        [FromBody] CompareLoanSavingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.CompareLoanSavingsAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("compare")]
    public async Task<IActionResult> Compare(
        [FromBody] ComparePayoffPlansRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.ComparePayoffPlansAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("compare-saved")]
    public async Task<IActionResult> CompareSaved(
        [FromBody] CompareSavedPayoffPlansRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.CompareSavedPayoffPlansAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("activate")]
    public async Task<IActionResult> Activate(
        [FromBody] ActivatePayoffPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.ActivatePayoffPlanAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await decisionService.GetActivePayoffPlanAsync(cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("active")]
    public async Task<IActionResult> ReviseActive(
        [FromBody] ReviseActivePayoffPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.ReviseActivePayoffPlanAsync(request, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("active/payments")]
    public async Task<IActionResult> RecordPayment(
        [FromBody] RecordPayoffPlanPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.RecordActivePayoffPlanPaymentAsync(request, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("active/payments/{payoffPlanPaymentId:int}")]
    public async Task<IActionResult> VoidPayment(
        int payoffPlanPaymentId,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.VoidActivePayoffPlanPaymentAsync(
            payoffPlanPaymentId,
            cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("active/complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var result = await decisionService.CompleteActivePayoffPlanAsync(cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("active/abandon")]
    public async Task<IActionResult> Abandon(CancellationToken cancellationToken)
    {
        var result = await decisionService.AbandonActivePayoffPlanAsync(cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("active/history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var result = await decisionService.GetActivePayoffPlanHistoryAsync(cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("active/progress")]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
    {
        var result = await decisionService.GetActivePayoffPlanProgressAsync(cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }
}
