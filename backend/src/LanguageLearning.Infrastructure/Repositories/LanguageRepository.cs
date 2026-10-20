
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class LanguageRepository : ILanguageRepository
{
    private readonly AppDbContext _context;

    public LanguageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Language>> GetActiveLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .AsNoTracking()
            .Where(language => language.IsActive)
            .OrderBy(language => language.Name)
            .ToListAsync(cancellationToken);
    }
}
