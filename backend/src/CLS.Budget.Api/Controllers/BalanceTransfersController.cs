using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/balance-transfers")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public sealed class BalanceTransfersController(ICreditCardDecisionService decisionService) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] AnalyzeBalanceTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.AnalyzeBalanceTransferAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
