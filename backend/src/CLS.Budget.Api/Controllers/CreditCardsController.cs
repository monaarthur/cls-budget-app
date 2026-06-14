using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/creditcards")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class CreditCardsController(ICreditCardService creditCardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await creditCardService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await creditCardService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await creditCardService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.AccountId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await creditCardService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize(Policy = AuthorizationPolicies.TenantOwner)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await creditCardService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : NotFound(result);
    }
}
