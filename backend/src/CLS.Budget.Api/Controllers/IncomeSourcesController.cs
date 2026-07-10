using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.IncomeSources.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CLS.Budget.Api.Auth;

namespace CLS.Budget.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/income-sources")]
public class IncomeSourcesController(IIncomeSourceService incomeSourceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await incomeSourceService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TenantMember)]
    [ProducesResponseType(typeof(ApiResponse<IncomeSourceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IncomeSourceResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIncomeSourceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await incomeSourceService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetAll), result);
    }
}
