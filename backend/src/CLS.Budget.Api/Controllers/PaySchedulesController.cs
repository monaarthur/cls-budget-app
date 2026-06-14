using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/pay-frequency-types")]
public class PayFrequencyTypesController(IPayFrequencyTypeService payFrequencyTypeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await payFrequencyTypeService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/pay-schedules")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class PaySchedulesController(IPayScheduleService payScheduleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await payScheduleService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("default")]
    public async Task<IActionResult> GetDefault(CancellationToken cancellationToken)
    {
        var result = await payScheduleService.GetDefaultAsync(cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await payScheduleService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:int}/dates")]
    public async Task<IActionResult> GetDates(
        int id,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken cancellationToken)
    {
        var result = await payScheduleService.GetDatesAsync(id, start, end, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Application.PaySchedules.Dtos.CreatePayScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await payScheduleService.CreateAsync(request, cancellationToken);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.PayScheduleId }, result)
            : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Application.PaySchedules.Dtos.UpdatePayScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await payScheduleService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
