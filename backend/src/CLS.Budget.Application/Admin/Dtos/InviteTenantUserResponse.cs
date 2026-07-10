namespace CLS.Budget.Application.Admin.Dtos;

public sealed class InviteTenantUserResponse
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool InviteSent { get; set; }

    /// <summary>Account setup URL (also emailed when SMTP is configured).</summary>
    public string SetupLink { get; set; } = null!;
}
