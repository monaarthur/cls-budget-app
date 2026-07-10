using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class AppUserRepository(BudgetDbContext dbContext) : IAppUserRepository
{
    public async Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.AppUsers.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<AppUser> AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        dbContext.AppUsers.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
