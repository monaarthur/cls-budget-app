namespace CLS.Budget.Application.Auth.Dtos;

/// <summary>
/// Registers a brand new tenant (household) and its first Owner user.
/// </summary>
public sealed class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Optional household/tenant name. Defaults to "{DisplayName}'s Household" when omitted.
    /// </summary>
    public string? TenantName { get; set; }
}
