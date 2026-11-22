using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Infrastructure.Repositories;

public class VocabularyRepository : IVocabularyRepository
{
    private readonly AppDbContext _context;

    public VocabularyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vocabulary>> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _context.Vocabularies
            .Where(v => v.LessonId == lessonId)
            .OrderBy(v => v.Term)
            .ToListAsync(cancellationToken);
    }

    public Task<Guid?> GetLessonLanguageIdAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
        _context.Lessons.Where(lesson => lesson.Id == lessonId)
            .Select(lesson => (Guid?)lesson.Unit.Course.LanguageId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<List<Vocabulary>> SearchAsync(Guid? languageId, string? term, int limit = 50, CancellationToken cancellationToken = default, string? level = null, IReadOnlyCollection<string>? domains = null)
    {
        var query = _context.Vocabularies.AsQueryable();

        if (languageId.HasValue && languageId != Guid.Empty)
        {
            query = query.Where(v => v.LanguageId == languageId.Value);
        }
        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalizedDomains = (domains ?? []).Select(domain => domain.ToLower()).ToArray();
            var normalizedLevel = level.ToLower();
            query = query.Where(v => v.Lesson.Status == ContentStatus.Published &&
                v.Lesson.Unit.Course.Status == ContentStatus.Published &&
                v.Lesson.Unit.Course.Level.ToLower() == normalizedLevel &&
                normalizedDomains.Contains(v.Lesson.Unit.Course.Domain.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var lowerTerm = term.Trim().ToLower();
            query = query.Where(v => v.Term.ToLower().Contains(lowerTerm) || v.Meaning.ToLower().Contains(lowerTerm));
        }

        return await query.OrderBy(v => v.Term).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<Vocabulary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vocabularies
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default)
    {
        await _context.Vocabularies.AddAsync(vocabulary, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default)
    {
        _context.Vocabularies.Update(vocabulary);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default)
    {
        _context.Vocabularies.Remove(vocabulary);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
