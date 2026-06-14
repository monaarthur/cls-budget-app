namespace CLS.Budget.Application.Auth.Dtos;

public sealed class CurrentUserResponse
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = null!;
}
