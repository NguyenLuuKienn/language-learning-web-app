using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Repositories;

public class QuizRepository : IQuizRepository
{
    private readonly AppDbContext _context;

    public QuizRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<QuizAttempt?> GetAttemptByClientIdAsync(Guid userId, Guid clientAttemptId, CancellationToken cancellationToken = default) =>
        _context.QuizAttempts.AsNoTracking().Include(item => item.Answers)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.ClientAttemptId == clientAttemptId, cancellationToken);

    public async Task<List<Question>> GetQuestionsByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Include(q => q.Options)
            .Where(q => q.LessonId == lessonId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasQuestionsAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
        _context.Questions.AnyAsync(question => question.LessonId == lessonId, cancellationToken);

    public async Task<Question?> GetQuestionWithDetailsAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
    }

    public async Task<bool> AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _context.QuizAttempts.AddAsync(attempt, cancellationToken);
        try { await _context.SaveChangesAsync(cancellationToken); return true; }
        catch (DbUpdateException) when (attempt.ClientAttemptId.HasValue)
        {
            _context.ChangeTracker.Clear();
            if (await _context.QuizAttempts.AsNoTracking().AnyAsync(item =>
                item.UserId == attempt.UserId && item.ClientAttemptId == attempt.ClientAttemptId, cancellationToken))
                return false;
            throw;
        }
    }

    public async Task<List<QuizAttempt>> GetUserAttemptsAsync(Guid userId, Guid? lessonId = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.QuizAttempts
            .Include(a => a.Lesson)
            .Where(a => a.UserId == userId);

        if (lessonId.HasValue && lessonId != Guid.Empty)
        {
            query = query.Where(a => a.LessonId == lessonId.Value);
        }

        return await query.OrderByDescending(a => a.CompletedAt).Take(limit).ToListAsync(cancellationToken);
    }
}
