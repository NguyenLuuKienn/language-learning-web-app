using LanguageLearning.Application.Common;
using System.Text.Json;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class StudyTargetService : IStudyTargetService
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IPlacementProofService _proofs;

    public StudyTargetService(IUserRepository users, ICourseRepository courses, IPlacementProofService proofs)
    {
        _users = users;
        _courses = courses;
        _proofs = proofs;
    }

    public async Task<StudyTargetDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(Messages.Auth.UserNotFound);
        return string.IsNullOrWhiteSpace(user.StudyTargetJson)
            ? null
            : JsonSerializer.Deserialize<StudyTargetDto>(user.StudyTargetJson);
    }

    public async Task<StudyTargetDto> SaveAsync(Guid userId, StudyTargetDto request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(Messages.Auth.UserNotFound);
        var language = await _courses.GetLanguageByIdAsync(request.LanguageId, cancellationToken)
            ?? throw new ArgumentException(Messages.Course.LanguageNotFound);
        if (!language.IsActive) throw new ArgumentException(Messages.Course.LanguageInactive);
        var domains = (request.Domains?.Count > 0 ? request.Domains : new List<string> { request.Domain })
            .Select(domain => domain.Trim().ToLowerInvariant())
            .Where(domain => domain.Length > 0)
            .Distinct()
            .ToList();
        if (domains.Count == 0 || string.IsNullOrWhiteSpace(request.Level))
            throw new ArgumentException(Messages.Course.LevelAndDomainRequired);

        var courses = await _courses.GetCourseSummariesAsync(language.Id, request.Level.Trim(), null, userId, cancellationToken);
        if (!domains.All(domain => courses.Any(course => course.HasMaterial && course.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException(Messages.Course.NoMatchingMaterial);

        var existing = string.IsNullOrWhiteSpace(user.StudyTargetJson)
            ? null : JsonSerializer.Deserialize<StudyTargetDto>(user.StudyTargetJson);
        var changingLevel = existing == null || existing.LanguageId != language.Id
            || !string.Equals(existing.Level, request.Level, StringComparison.OrdinalIgnoreCase);
        if (user.Role != UserRole.Admin && changingLevel && !courses.Any(course => course.HasMaterial &&
                _proofs.IsValid(request.PlacementProof, userId, course.Id)))
            throw new ArgumentException(Messages.Course.PlacementProofRequired);

        var target = new StudyTargetDto
        {
            LanguageId = language.Id,
            LanguageCode = language.Code,
            LanguageName = language.Name,
            NativeName = language.NativeName,
            FlagEmoji = language.FlagEmoji,
            Level = request.Level.Trim(),
            Domain = domains[0],
            Domains = domains
        };
        user.StudyTargetJson = JsonSerializer.Serialize(target);
        await _users.UpdateAsync(user, cancellationToken);
        return target;
    }
}
