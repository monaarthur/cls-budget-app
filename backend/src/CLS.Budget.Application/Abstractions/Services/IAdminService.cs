using CLS.Budget.Application.Admin.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IAdminService
{
    Task<ApiResponse<IReadOnlyList<TenantSummaryResponse>>> ListTenantsAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InviteTenantUserResponse>> InviteUserToTenantAsync(
        InviteTenantUserRequest request,
        CancellationToken cancellationToken = default);
}
