namespace CLS.Budget.Domain.Entities;

/// <summary>
/// A hashed, expiring one-time token used to reset an <see cref="AppUser"/> password.
/// </summary>
public class PasswordResetToken
{
    public Guid PasswordResetTokenId { get; set; }
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
