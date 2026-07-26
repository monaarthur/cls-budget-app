using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/cash-flow")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public sealed class CashFlowController(ICreditCardDecisionService decisionService) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] AnalyzeCashFlowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.AnalyzeCashFlowAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
