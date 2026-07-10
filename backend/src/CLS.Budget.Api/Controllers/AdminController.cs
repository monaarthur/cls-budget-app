using CLS.Budget.Api.Admin;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[AllowAnonymous]
[AdminApiKey]
[Route("api/v1/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("tenants")]
    public async Task<IActionResult> ListTenants(CancellationToken cancellationToken)
    {
        var result = await adminService.ListTenantsAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("tenant-users/invite")]
    public async Task<IActionResult> InviteTenantUser(
        [FromBody] InviteTenantUserRequest invite,
        CancellationToken cancellationToken)
    {
        var result = await adminService.InviteUserToTenantAsync(invite, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
