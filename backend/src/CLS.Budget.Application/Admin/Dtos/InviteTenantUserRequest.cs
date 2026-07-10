namespace CLS.Budget.Application.Admin.Dtos;

public sealed class InviteTenantUserRequest
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = "Owner";
}
