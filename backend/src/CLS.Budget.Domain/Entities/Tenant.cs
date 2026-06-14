namespace CLS.Budget.Domain.Entities;

/// <summary>
/// An isolation boundary that owns budget data (a household or organization).
/// </summary>
public class Tenant
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
