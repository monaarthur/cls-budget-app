using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.AutoPayoff.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Api.Auth;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.TenantMember)]
[ApiController]
[Route("api/v1/auto-payoff")]
public class AutoPayoffController(
    IAutoPayoffService autoPayoffService,
    IValidator<SaveAutoPayoffConfigRequest> validator) : ControllerBase
{
    [HttpGet("configs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AutoPayoffConfigResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await autoPayoffService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<AutoPayoffConfigResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var result = await autoPayoffService.GetConfigAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("config/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AutoPayoffConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AutoPayoffConfigResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfigById(int id, CancellationToken cancellationToken)
    {
        var result = await autoPayoffService.GetConfigAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("config")]
    [ProducesResponseType(typeof(ApiResponse<AutoPayoffConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AutoPayoffConfigResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveConfig(
        [FromBody] SaveAutoPayoffConfigRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AutoPayoffConfigResponse>.Fail(
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await autoPayoffService.SaveConfigAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(ApiResponse<AutoPayoffPreviewResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(
        [FromBody, CustomizeValidator(Skip = true)] SaveAutoPayoffConfigRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await autoPayoffService.PreviewAsync(request, cancellationToken);
        return Ok(result);
    }
}
