namespace CLS.Budget.Infrastructure.Auth;

/// <summary>
/// JWT signing and lifetime settings, bound from the <c>Jwt</c> configuration section.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CLS.Budget";
    public string Audience { get; set; } = "CLS.Budget";

    /// <summary>Symmetric signing key. Must be at least 32 bytes for HMAC-SHA256.</summary>
    public string SigningKey { get; set; } = null!;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
