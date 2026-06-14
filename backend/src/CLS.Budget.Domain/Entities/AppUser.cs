namespace CLS.Budget.Domain.Entities;

/// <summary>
/// An application user that belongs to a single <see cref="Tenant"/>.
/// </summary>
public class AppUser
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public TenantRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
