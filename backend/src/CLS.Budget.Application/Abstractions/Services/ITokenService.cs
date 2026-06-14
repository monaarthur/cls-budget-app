using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Services;

/// <summary>
/// Issues signed JWT access tokens and opaque refresh tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(AppUser user);

    /// <summary>
    /// Generates a new opaque refresh token plus the hash to persist and its expiry.
    /// The raw value is returned to the caller once and never stored.
    /// </summary>
    RefreshTokenResult CreateRefreshToken();

    /// <summary>
    /// Hashes a raw refresh token so it can be matched against the stored hash.
    /// </summary>
    string HashRefreshToken(string rawToken);
}

/// <summary>A signed JWT access token and the instant it expires (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>A freshly minted refresh token: the raw value, its stored hash, and expiry (UTC).</summary>
public sealed record RefreshTokenResult(string RawToken, string TokenHash, DateTime ExpiresAt);
