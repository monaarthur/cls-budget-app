namespace CLS.Budget.Application.Abstractions;

/// <summary>
/// Hashes and verifies user passwords. Backed by ASP.NET Core's
/// <c>PasswordHasher</c> in the infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="password"/> matches <paramref name="hash"/>.
    /// </summary>
    bool Verify(string hash, string password);
}
