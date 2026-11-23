using LanguageLearning.Application.Interfaces;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class AccountStatus(AppDbContext db) : IAccountStatus
{
    public Task<bool> IsActiveAsync(Guid userId, CancellationToken ct) =>
        db.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.IsActive, ct);
}
