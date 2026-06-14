using CLS.Budget.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
