using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<PasswordResetToken> AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task InvalidateActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
