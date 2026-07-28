using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.AccountCategories.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/account-categories")]
public class AccountCategoriesController(IAccountCategoryService accountCategoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AccountCategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await accountCategoryService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TenantMember)]
    [ProducesResponseType(typeof(ApiResponse<AccountCategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AccountCategoryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateAccountCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountCategoryService.CreateCategoryAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPost("subcategories")]
    [Authorize(Policy = AuthorizationPolicies.TenantMember)]
    [ProducesResponseType(typeof(ApiResponse<AccountSubCategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AccountSubCategoryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubCategory(
        [FromBody] CreateAccountSubCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountCategoryService.CreateSubCategoryAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetAll), result);
    }
}
