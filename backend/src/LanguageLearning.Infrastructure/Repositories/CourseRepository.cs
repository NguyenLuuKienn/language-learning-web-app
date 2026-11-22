using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Data;
using LanguageLearning.Application.Common.Exceptions;

namespace LanguageLearning.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _context;

    public CourseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Language?> GetLanguageByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Language?> GetLanguageByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.Code.ToLower() == code.ToLower(), cancellationToken);
    }

    public async Task AddLanguageAsync(Language language, CancellationToken cancellationToken = default)
    {
        await _context.Languages.AddAsync(language, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLanguageAsync(Language language, CancellationToken cancellationToken = default)
    {
        _context.Languages.Update(language);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLanguageAsync(Language language, CancellationToken cancellationToken = default)
    {
        _context.Languages.Remove(language);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CourseResponse>> GetCourseSummariesAsync(Guid? languageId, string? level, string? domain, Guid? userId, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses.AsNoTracking().Where(c => c.Status == ContentStatus.Published);
        if (languageId.HasValue && languageId.Value != Guid.Empty)
            query = query.Where(c => c.LanguageId == languageId.Value);
        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(c => c.Level.ToLower() == level.ToLower());
        if (!string.IsNullOrWhiteSpace(domain))
            query = query.Where(c => c.Domain.ToLower() == domain.ToLower());

        return await query.OrderBy(c => c.Title).Select(c => new CourseResponse
        {
            Id = c.Id,
            LanguageId = c.LanguageId,
            LanguageName = c.Language.Name,
            LanguageCode = c.Language.Code,
            Title = c.Title,
            Level = c.Level,
            Domain = c.Domain,
            Description = c.Description,
            Status = c.Status,
            UnitsCount = c.Units.Count,
            LessonsCount = c.Units.SelectMany(u => u.Lessons).Count(),
            IsEnrolled = userId.HasValue && c.Enrollments.Any(e => e.UserId == userId.Value),
            HasMaterial = c.MaterialJson != null && c.MaterialJson != "" && c.Units.Any()
        }).ToListAsync(cancellationToken);
    }

    public async Task<List<Course>> GetAllCoursesForExportAsync(Guid? languageId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Include(c => c.Language)
            .Include(c => c.Units)
                .ThenInclude(u => u.Lessons)
                    .ThenInclude(l => l.Vocabularies)
            .AsSplitQuery()
            .AsQueryable();

        if (languageId.HasValue && languageId != Guid.Empty)
        {
            query = query.Where(c => c.LanguageId == languageId.Value);
        }

        return await query.OrderBy(c => c.Title).ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetCourseByIdAsync(Guid id, bool includeUnitsAndLessons = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Include(c => c.Language)
            .AsQueryable();

        if (includeUnitsAndLessons)
        {
            query = query
                .Include(c => c.Units)
                    .ThenInclude(u => u.Lessons)
                        .ThenInclude(l => l.Vocabularies)
                .AsSplitQuery();
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddCourseAsync(Course course, CancellationToken cancellationToken = default)
    {
        await _context.Courses.AddAsync(course, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCourseAsync(Course course, CancellationToken cancellationToken = default)
    {
        _context.Courses.Update(course);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCourseMaterialAsync(Course course, List<Unit> units, List<Deck> decks, CancellationToken cancellationToken = default)
    {
        _context.Courses.Update(course);
        await _context.Units.AddRangeAsync(units, cancellationToken);
        await _context.Decks.AddRangeAsync(decks, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveEditedLessonMaterialAsync(
        Course course, Guid lessonId, IReadOnlyList<Vocabulary> vocabularies, IReadOnlyList<string> vocabularyMaterialIds,
        IReadOnlyList<Question> questions, IReadOnlyDictionary<string, (string Term, string Meaning)> oldVocabulary,
        IReadOnlyDictionary<string, string> oldQuestionPrompts, CancellationToken cancellationToken = default)
    {
        var existingVocabulary = await _context.Vocabularies.Where(item => item.LessonId == lessonId)
            .OrderBy(item => item.Id).ToListAsync(cancellationToken);
        var retainedVocabulary = new HashSet<Guid>();
        for (var index = 0; index < vocabularies.Count; index++)
        {
            var desired = vocabularies[index];
            var hasOld = oldVocabulary.TryGetValue(vocabularyMaterialIds[index], out var old);
            var current = hasOld ? existingVocabulary.FirstOrDefault(item => !retainedVocabulary.Contains(item.Id) &&
                item.Term == old.Term && item.Meaning == old.Meaning) : null;
            if (current == null && hasOld)
                current = existingVocabulary.FirstOrDefault(item => !retainedVocabulary.Contains(item.Id) && item.Term == old.Term);
            if (current == null && !hasOld)
            {
                await _context.Vocabularies.AddAsync(desired, cancellationToken);
                continue;
            }
            if (current == null)
                throw new InvalidOperationException("The stored vocabulary no longer matches the lesson material. Reload before editing.");
            retainedVocabulary.Add(current.Id);
            current.Term = desired.Term;
            current.Meaning = desired.Meaning;
            current.MeaningsJson = desired.MeaningsJson;
            current.PartOfSpeech = desired.PartOfSpeech;
            current.Phonetic = desired.Phonetic;
            current.Example = desired.Example;
            current.AudioUrl = desired.AudioUrl;
            current.ImageUrl = desired.ImageUrl;
        }
        _context.Vocabularies.RemoveRange(existingVocabulary.Where(item => !retainedVocabulary.Contains(item.Id)));

        var existingQuestions = await _context.Questions.Include(item => item.Options)
            .Where(item => item.LessonId == lessonId).ToListAsync(cancellationToken);
        var retained = new HashSet<Guid>();
        foreach (var desired in questions)
        {
            var materialId = GetMaterialQuestionId(desired.ConfigJson);
            var oldPrompt = materialId != null && oldQuestionPrompts.TryGetValue(materialId, out var prompt) ? prompt : null;
            var current = existingQuestions.FirstOrDefault(item => !retained.Contains(item.Id)
                && (materialId != null && GetMaterialQuestionId(item.ConfigJson) == materialId
                    || oldPrompt != null && item.Prompt == oldPrompt));
            if (current == null && materialId == null && oldQuestionPrompts.Count == 0)
                current = existingQuestions.FirstOrDefault(item => !retained.Contains(item.Id));
            if (current == null)
            {
                await _context.Questions.AddAsync(desired, cancellationToken);
                continue;
            }
            retained.Add(current.Id);
            current.Prompt = desired.Prompt;
            current.Explanation = desired.Explanation;
            current.ConfigJson = desired.ConfigJson;
            var currentOptions = current.Options.OrderBy(item => item.SortOrder).ToList();
            var desiredOptions = desired.Options.OrderBy(item => item.SortOrder).ToList();
            for (var index = 0; index < desiredOptions.Count; index++)
            {
                var option = desiredOptions[index];
                if (index >= currentOptions.Count)
                    current.Options.Add(new QuestionOption { Id = Guid.NewGuid(), QuestionId = current.Id, Content = option.Content, SortOrder = index, IsCorrect = option.IsCorrect });
                else
                {
                    currentOptions[index].Content = option.Content;
                    currentOptions[index].IsCorrect = option.IsCorrect;
                }
            }
            if (currentOptions.Count > desiredOptions.Count)
                _context.QuestionOptions.RemoveRange(currentOptions.Skip(desiredOptions.Count));
        }
        var removedQuestions = existingQuestions.Where(item => !retained.Contains(item.Id)).ToList();
        if (removedQuestions.Count > 0)
        {
            var removedIds = removedQuestions.Select(item => item.Id).ToList();
            if (await _context.QuizAnswers.AnyAsync(item => removedIds.Contains(item.QuestionId), cancellationToken))
                throw new InvalidOperationException("Questions with submitted answers cannot be deleted.");
            _context.Questions.RemoveRange(removedQuestions);
        }
        _context.Entry(course).Property(item => item.MaterialJson).IsModified = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemapLessonStepsAsync(Guid courseId, Guid lessonId, string expectedMaterialJson,
        string updatedMaterialJson, IReadOnlyDictionary<string, string> stepIds, string newContentVersion,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var storedMaterial = await _context.Courses.AsNoTracking().Where(item => item.Id == courseId)
                .Select(item => item.MaterialJson).SingleAsync(cancellationToken);
            if (storedMaterial != expectedMaterialJson)
                throw new BusinessRuleException("LESSON_CONTENT_CHANGED", "Course material changed during the migration. Reload before retrying.");
            var course = await _context.Courses.SingleAsync(item => item.Id == courseId, cancellationToken);
            var stepProgress = await _context.LessonStepProgresses
                .Where(item => item.LessonId == lessonId).ToListAsync(cancellationToken);
            foreach (var item in stepProgress)
                if (stepIds.TryGetValue(item.StepId, out var target))
                {
                    item.StepId = target;
                    item.Revision++;
                }
            var lessonProgress = await _context.LessonProgresses
                .Where(item => item.LessonId == lessonId).ToListAsync(cancellationToken);
            foreach (var item in lessonProgress)
                if (item.CurrentStepId != null && stepIds.TryGetValue(item.CurrentStepId, out var target))
                    item.CurrentStepId = target;
            var evidences = await _context.LearningEvidences
                .Where(item => item.LessonId == lessonId).ToListAsync(cancellationToken);
            foreach (var item in evidences)
            {
                if (stepIds.TryGetValue(item.StepId, out var target)) item.StepId = target;
                item.ContentVersion = newContentVersion;
            }
            var sessions = await _context.LearningSessions
                .Where(item => item.LessonId == lessonId).ToListAsync(cancellationToken);
            foreach (var item in sessions) item.ContentVersion = newContentVersion;
            course.MaterialJson = updatedMaterialJson;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static string? GetMaterialQuestionId(string config)
    {
        try
        {
            using var document = JsonDocument.Parse(config);
            return document.RootElement.TryGetProperty("materialId", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task DeleteCourseAsync(Course course, CancellationToken cancellationToken = default)
    {
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Unit?> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Units
            .Include(u => u.Lessons)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddUnitAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        await _context.Units.AddAsync(unit, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUnitAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        _context.Units.Update(unit);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUnitAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        _context.Units.Remove(unit);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Lesson?> GetLessonByIdAsync(Guid id, bool includeVocabularies = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Lessons.Include(l => l.Unit).ThenInclude(u => u.Course).AsQueryable();

        if (includeVocabularies)
        {
            query = query.Include(l => l.Vocabularies);
        }

        return await query.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task AddLessonAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        await _context.Lessons.AddAsync(lesson, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLessonAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _context.Lessons.Update(lesson);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLessonAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseEnrollments
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken);
    }

    public async Task EnrollAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken);

        if (existing == null)
        {
            await _context.CourseEnrollments.AddAsync(new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<LessonProgress>> GetUserLessonProgressesAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _context.LessonProgresses
            .Include(p => p.Lesson)
                .ThenInclude(l => l.Unit)
            .Where(p => p.UserId == userId && p.Lesson.Unit.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }
}
