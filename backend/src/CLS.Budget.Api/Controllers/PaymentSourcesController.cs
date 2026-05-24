using CLS.Budget.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/payment-sources")]
public class PaymentSourcesController(IPaymentSourceService paymentSourceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await paymentSourceService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
