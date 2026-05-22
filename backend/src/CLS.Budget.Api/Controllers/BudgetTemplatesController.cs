using CLS.Budget.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/budget-templates")]
public class BudgetTemplatesController(IBudgetTemplateService budgetTemplateService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await budgetTemplateService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
