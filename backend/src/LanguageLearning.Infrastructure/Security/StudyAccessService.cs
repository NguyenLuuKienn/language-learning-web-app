using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Security;

public sealed class StudyAccessService : IStudyAccessService
{
    private readonly AppDbContext _db;
    private readonly IStudyTargetService _targets;
    private Guid _cachedUserId;
    private bool _loaded;
    private StudyTargetDto? _target;

    public StudyAccessService(AppDbContext db, IStudyTargetService targets)
    {
        _db = db;
        _targets = targets;
    }

    public async Task<StudyTargetDto?> GetTargetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_loaded || _cachedUserId != userId)
        {
            _target = await _targets.GetAsync(userId, cancellationToken);
            _cachedUserId = userId;
            _loaded = true;
        }
        return _target;
    }

    public async Task<bool> CanAccessDeckAsync(Guid userId, Guid deckId, CancellationToken cancellationToken = default)
    {
        var target = await GetTargetAsync(userId, cancellationToken);
        return target != null && await _db.Decks.AsNoTracking().AnyAsync(deck =>
            deck.Id == deckId && !deck.IsDeleted && deck.LanguageId == target.LanguageId &&
            (deck.OwnerId == userId || (deck.IsPublic && deck.Tags.Any(tag => tag.Tag.ToLower() == "course-level:" + target.Level.ToLower()))) &&
            !deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") &&
                tag.Tag.ToLower() != "course-level:" + target.Level.ToLower()), cancellationToken);
    }

    public async Task<bool> CanAccessCardAsync(Guid userId, Guid cardId, CancellationToken cancellationToken = default)
    {
        var deckId = await _db.Cards.AsNoTracking().Where(card => card.Id == cardId)
            .Select(card => (Guid?)card.DeckId).FirstOrDefaultAsync(cancellationToken);
        return deckId.HasValue && await CanAccessDeckAsync(userId, deckId.Value, cancellationToken);
    }

    public async Task<bool> CanAccessLessonAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var courseId = await _db.Lessons.AsNoTracking().Where(lesson => lesson.Id == lessonId && lesson.Status == ContentStatus.Published)
            .Select(lesson => (Guid?)lesson.Unit.CourseId).FirstOrDefaultAsync(cancellationToken);
        return courseId.HasValue && await CanAccessCourseAsync(userId, courseId.Value, cancellationToken);
    }

    public async Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var target = await GetTargetAsync(userId, cancellationToken);
        if (target == null) return false;
        var domains = target.Domains?.Count > 0 ? target.Domains : [target.Domain];
        var normalized = domains.Select(domain => domain.ToLower()).ToArray();
        return await _db.Courses.AsNoTracking().AnyAsync(course => course.Id == courseId &&
            course.Status == ContentStatus.Published && course.LanguageId == target.LanguageId &&
            course.Level.ToLower() == target.Level.ToLower() && normalized.Contains(course.Domain.ToLower()), cancellationToken);
    }

    public async Task<bool> CanAccessVocabularyAsync(Guid userId, Guid vocabularyId, CancellationToken cancellationToken = default)
    {
        var lessonId = await _db.Vocabularies.AsNoTracking().Where(vocabulary => vocabulary.Id == vocabularyId)
            .Select(vocabulary => (Guid?)vocabulary.LessonId).FirstOrDefaultAsync(cancellationToken);
        return lessonId.HasValue && await CanAccessLessonAsync(userId, lessonId.Value, cancellationToken);
    }

    public async Task<bool> CanAccessMediaAsync(Guid userId, string fileName, CancellationToken cancellationToken = default)
    {
        if (await _db.PlacementAudios.AsNoTracking().AnyAsync(audio =>
            audio.UserId == userId && audio.FileName == fileName, cancellationToken)) return true;
        var url = "/api/v1/media/" + fileName;
        if (await _db.SpecialPracticeAttempts.AsNoTracking().AnyAsync(attempt =>
            attempt.UserId == userId && attempt.AnswersJson.Contains(url), cancellationToken)) return true;
        var target = await GetTargetAsync(userId, cancellationToken);
        if (target == null) return false;
        var domains = target.Domains?.Count > 0 ? target.Domains : [target.Domain];
        var normalized = domains.Select(domain => domain.ToLower()).ToArray();
        return await _db.Vocabularies.AsNoTracking().AnyAsync(vocabulary =>
            (vocabulary.AudioUrl == url || vocabulary.ImageUrl == url) &&
            vocabulary.LanguageId == target.LanguageId && vocabulary.Lesson.Status == ContentStatus.Published &&
            vocabulary.Lesson.Unit.Course.Status == ContentStatus.Published &&
            vocabulary.Lesson.Unit.Course.Level.ToLower() == target.Level.ToLower() &&
            normalized.Contains(vocabulary.Lesson.Unit.Course.Domain.ToLower()), cancellationToken)
            || await _db.Courses.AsNoTracking().AnyAsync(course =>
                course.LanguageId == target.LanguageId && course.Level.ToLower() == target.Level.ToLower() &&
                normalized.Contains(course.Domain.ToLower()) && course.Status == ContentStatus.Published &&
                course.MaterialJson != null && course.MaterialJson.Contains(url), cancellationToken)
            || await _db.Cards.AsNoTracking().AnyAsync(card =>
                (card.AudioUrl == url || card.ImageUrl == url) && card.Deck.LanguageId == target.LanguageId &&
                !card.Deck.IsDeleted && (card.Deck.OwnerId == userId ||
                    (card.Deck.IsPublic && card.Deck.Tags.Any(tag => tag.Tag.ToLower() == "course-level:" + target.Level.ToLower()))) &&
                !card.Deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:") &&
                    tag.Tag.ToLower() != "course-level:" + target.Level.ToLower()), cancellationToken);
    }
}
