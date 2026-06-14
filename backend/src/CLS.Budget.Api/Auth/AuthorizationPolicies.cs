namespace CLS.Budget.Api.Auth;

/// <summary>
/// Named authorization policies and role constants aligned with <see cref="Domain.Entities.TenantRole"/>.
/// </summary>
public static class AuthorizationPolicies
{
    public const string TenantMember = nameof(TenantMember);
    public const string TenantOwner = nameof(TenantOwner);

    public const string MemberRole = "Member";
    public const string OwnerRole = "Owner";
}
