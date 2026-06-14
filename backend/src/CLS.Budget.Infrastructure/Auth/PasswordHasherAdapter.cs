using CLS.Budget.Application.Abstractions;
using CLS.Budget.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CLS.Budget.Infrastructure.Auth;

/// <summary>
/// Adapts ASP.NET Core's <see cref="PasswordHasher{TUser}"/> to the application's
/// <see cref="IPasswordHasher"/> abstraction.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new AppUser(), password);

    public bool Verify(string hash, string password)
    {
        var result = _hasher.VerifyHashedPassword(new AppUser(), hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
