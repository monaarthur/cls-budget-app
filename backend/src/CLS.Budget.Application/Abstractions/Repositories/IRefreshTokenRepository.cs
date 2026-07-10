using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>Looks up a refresh token by its stored hash, including the owning user.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
