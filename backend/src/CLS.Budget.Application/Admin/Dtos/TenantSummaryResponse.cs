namespace CLS.Budget.Application.Admin.Dtos;

public sealed class TenantSummaryResponse
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public IReadOnlyList<string> UserEmails { get; set; } = [];
}
