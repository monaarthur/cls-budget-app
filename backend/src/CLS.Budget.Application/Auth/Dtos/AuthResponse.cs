namespace CLS.Budget.Application.Auth.Dtos;

/// <summary>
/// Result of a successful register/login/refresh: an access token (with expiry),
/// a refresh token, and the authenticated user's profile.
/// </summary>
public sealed class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public CurrentUserResponse User { get; set; } = null!;
}
