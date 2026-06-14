using CLS.Budget.Application.Auth.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Auth;

internal static class AuthMapper
{
    public static CurrentUserResponse ToCurrentUser(AppUser user) => new()
    {
        UserId = user.UserId,
        TenantId = user.TenantId,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Role = user.Role.ToString()
    };
}
