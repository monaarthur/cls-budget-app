using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/credit-cards")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public sealed class CreditCardDecisionController(ICreditCardDecisionService decisionService) : ControllerBase
{
    [HttpPost("loan-schedule")]
    public async Task<IActionResult> BuildLoanSchedule(
        [FromBody] LoanScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.BuildLoanScheduleAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("utilization-summary")]
    public async Task<IActionResult> GetUtilizationSummary(CancellationToken cancellationToken)
    {
        var result = await decisionService.GetUtilizationSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/interest-analysis")]
    public async Task<IActionResult> AnalyzeInterest(
        int id,
        [FromBody] InterestAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.AnalyzeInterestAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }
}
