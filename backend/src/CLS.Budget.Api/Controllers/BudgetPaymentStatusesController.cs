using CLS.Budget.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/budget-payment-statuses")]
public class BudgetPaymentStatusesController(IBudgetPaymentStatusService budgetPaymentStatusService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await budgetPaymentStatusService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
