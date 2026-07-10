using System.Security.Cryptography;
using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Admin.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Admin;

public sealed class AdminService(
    ITenantRepository tenantRepository,
    IAppUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IPasswordResetNotifier passwordResetNotifier,
    IPasswordResetSettings passwordResetSettings) : IAdminService
{
    public async Task<ApiResponse<IReadOnlyList<TenantSummaryResponse>>> ListTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenants = await tenantRepository.ListAsync(cancellationToken);

        var summaries = tenants
            .Select(t => new TenantSummaryResponse
            {
                TenantId = t.TenantId,
                Name = t.Name,
                IsActive = t.IsActive,
                UserCount = t.Users.Count,
                UserEmails = t.Users
                    .OrderBy(u => u.Email)
                    .Select(u => u.Email)
                    .ToList()
            })
            .ToList();

        return ApiResponse<IReadOnlyList<TenantSummaryResponse>>.Ok(summaries);
    }

    public async Task<ApiResponse<InviteTenantUserResponse>> InviteUserToTenantAsync(
        InviteTenantUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = Normalize(request.Email);
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);

        if (tenant is null)
        {
            return ApiResponse<InviteTenantUserResponse>.Fail("Tenant was not found.");
        }

        if (!tenant.IsActive)
        {
            return ApiResponse<InviteTenantUserResponse>.Fail("Tenant is not active.");
        }

        var existing = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            if (existing.TenantId != tenant.TenantId)
            {
                return ApiResponse<InviteTenantUserResponse>.Fail(
                    "An account with that email already exists on another tenant.");
            }

            var (resent, resentSetupLink) = await SendSetPasswordInviteAsync(existing, cancellationToken);

            return ApiResponse<InviteTenantUserResponse>.Ok(
                new InviteTenantUserResponse
                {
                    UserId = existing.UserId,
                    TenantId = existing.TenantId,
                    Email = existing.Email,
                    DisplayName = existing.DisplayName,
                    InviteSent = resent,
                    SetupLink = resentSetupLink
                });
        }

        var now = DateTime.UtcNow;
        var user = await userRepository.AddAsync(
            new AppUser
            {
                UserId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                Email = email,
                PasswordHash = passwordHasher.Hash(CreateTemporaryPassword()),
                DisplayName = request.DisplayName.Trim(),
                Role = ParseRole(request.Role),
                IsActive = true,
                CreatedAt = now
            },
            cancellationToken);

        var (inviteSent, setupLink) = await SendSetPasswordInviteAsync(user, cancellationToken);

        return ApiResponse<InviteTenantUserResponse>.Ok(
            new InviteTenantUserResponse
            {
                UserId = user.UserId,
                TenantId = user.TenantId,
                Email = user.Email,
                DisplayName = user.DisplayName,
                InviteSent = inviteSent,
                SetupLink = setupLink
            });
    }

    private async Task<(bool InviteSent, string SetupLink)> SendSetPasswordInviteAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        await passwordResetTokenRepository.InvalidateActiveForUserAsync(
            user.UserId,
            cancellationToken);

        var reset = tokenService.CreateOneTimeToken(passwordResetSettings.TokenLifetime);
        await passwordResetTokenRepository.AddAsync(
            new PasswordResetToken
            {
                PasswordResetTokenId = Guid.NewGuid(),
                UserId = user.UserId,
                TokenHash = reset.TokenHash,
                ExpiresAt = reset.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        var setupLink = BuildSetupLink(reset.RawToken);

        await passwordResetNotifier.SendResetLinkAsync(
            user.Email,
            reset.RawToken,
            cancellationToken,
            isAccountInvite: true);

        return (true, setupLink);
    }

    private string BuildSetupLink(string rawToken)
    {
        var baseUrl = passwordResetSettings.FrontendBaseUrl.TrimEnd('/');
        return
            $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&invite=1";
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static TenantRole ParseRole(string role) =>
        role.Equals("Member", StringComparison.OrdinalIgnoreCase) || role == "0"
            ? TenantRole.Member
            : TenantRole.Owner;

    private static string CreateTemporaryPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
