namespace CLS.Budget.Domain;

/// <summary>
/// Fixed identity for the default seeded tenant. Matches the dev auth tenant id
/// (<c>Auth:DevTenantId</c>) so local development sees this tenant's data.
/// </summary>
public static class SeedTenant
{
    public static readonly Guid DefaultTenantId = new("00000000-0000-0000-0000-000000000001");
    public const string DefaultTenantName = "MonaArthur";
    public static readonly DateTime DefaultCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
