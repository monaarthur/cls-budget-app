using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(BudgetDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Update(token);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAt, now),
                cancellationToken);
    }
}
