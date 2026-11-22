using LanguageLearning.Application.DTOs.Auth;

namespace LanguageLearning.Application.Interfaces;

public interface IStudyTargetService
{
    Task<StudyTargetDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<StudyTargetDto> SaveAsync(Guid userId, StudyTargetDto request, CancellationToken cancellationToken = default);
}
