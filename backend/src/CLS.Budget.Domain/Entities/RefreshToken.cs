namespace CLS.Budget.Domain.Entities;

/// <summary>
/// A hashed, expiring refresh token used to mint new access tokens for an <see cref="AppUser"/>.
/// </summary>
public class RefreshToken
{
    public Guid RefreshTokenId { get; set; }
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
