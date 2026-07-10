using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository(BudgetDbContext dbContext) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        await dbContext.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<PasswordResetToken> AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        dbContext.PasswordResetTokens.Add(token);
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        dbContext.PasswordResetTokens.Update(token);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await dbContext.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.UsedAt, now),
                cancellationToken);
    }
}
