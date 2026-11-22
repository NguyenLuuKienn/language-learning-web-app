using LanguageLearning.Application.Common;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public class WritingSubmissionService : IWritingSubmissionService
{
    private readonly IWritingSubmissionRepository _repository;

    public WritingSubmissionService(IWritingSubmissionRepository repository) => _repository = repository;

    public async Task<WritingSubmissionDto?> GetLatestAsync(Guid userId, string promptId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptId)) throw new ArgumentException(Messages.Writing.PromptIdRequired);
        var submission = await _repository.GetLatestAsync(userId, promptId, cancellationToken);
        return submission is null ? null : Map(submission);
    }

    public async Task<WritingSubmissionDto> SaveAsync(Guid userId, SaveWritingSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PromptId) || request.PromptId.Length > 200)
            throw new ArgumentException(Messages.Writing.PromptIdInvalid);
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 20000)
            throw new ArgumentException(Messages.Writing.TextLengthInvalid);
        var submission = new WritingSubmission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromptId = request.PromptId.Trim(),
            Text = request.Text.Trim(),
            ClientSubmissionId = request.ClientSubmissionId
        };
        var saved = await _repository.AddAsync(submission, cancellationToken);
        if (saved.PromptId != submission.PromptId || saved.Text != submission.Text)
            throw new ArgumentException("This writing submission identifier was already used for different content.");
        return Map(saved);
    }

    private static WritingSubmissionDto Map(WritingSubmission submission) => new()
    {
        Id = submission.Id,
        PromptId = submission.PromptId,
        Text = submission.Text,
        CreatedAt = submission.CreatedAt
    };
}
