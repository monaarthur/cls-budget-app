using CLS.Budget.Domain;

namespace CLS.Budget.Api.Auth;

/// <summary>
/// Controls authentication enforcement, bound from the <c>Auth</c> configuration section.
/// When <see cref="Enabled"/> is <c>false</c> (Development only), requests are
/// auto-authenticated as the configured dev user/tenant via the dev auth handler.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>A fixed identity for the implicit dev user when auth is disabled.</summary>
    public static readonly Guid DefaultDevUserId = new("00000000-0000-0000-0000-0000000000aa");

    public bool Enabled { get; set; } = true;

    public Guid DevTenantId { get; set; } = SeedTenant.DefaultTenantId;
    public Guid DevUserId { get; set; } = DefaultDevUserId;
    public string DevEmail { get; set; } = "dev@localhost";
    public string DevDisplayName { get; set; } = "Dev User";
    public string DevRole { get; set; } = "Owner";
}
