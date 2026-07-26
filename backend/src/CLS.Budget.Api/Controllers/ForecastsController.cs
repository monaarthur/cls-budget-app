using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/forecasts")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public sealed class ForecastsController(ICreditCardDecisionService decisionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateForecastRequest request,
        CancellationToken cancellationToken)
    {
        var result = await decisionService.CreateForecastAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{forecastId:int}")]
    public async Task<IActionResult> Get(int forecastId, CancellationToken cancellationToken)
    {
        var result = await decisionService.GetForecastAsync(forecastId, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{forecastId:int}")]
    public async Task<IActionResult> Delete(int forecastId, CancellationToken cancellationToken)
    {
        var result = await decisionService.DeleteForecastAsync(forecastId, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }
}
