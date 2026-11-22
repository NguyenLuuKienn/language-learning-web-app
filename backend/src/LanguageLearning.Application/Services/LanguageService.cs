using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public sealed class LanguageService : ILanguageService
{
    private readonly ICourseRepository _courseRepository;

    public LanguageService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<List<LanguageResponse>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var languages = await _courseRepository.GetLanguagesAsync(cancellationToken);
        return languages.Select(l => new LanguageResponse
        {
            Id = l.Id,
            Code = l.Code,
            Name = l.Name,
            NativeName = l.NativeName,
            FlagEmoji = l.FlagEmoji,
            IsActive = l.IsActive
        }).ToList();
    }

    public async Task<LanguageResponse> CreateLanguageAsync(CreateLanguageRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _courseRepository.GetLanguageByCodeAsync(request.Code, cancellationToken);
        if (existing != null)
        {
            throw new ArgumentException(Messages.Course.LanguageCodeInUse);
        }

        var language = new Language
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToLower(),
            Name = request.Name.Trim(),
            NativeName = request.NativeName.Trim(),
            FlagEmoji = request.FlagEmoji.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddLanguageAsync(language, cancellationToken);

        return new LanguageResponse
        {
            Id = language.Id,
            Code = language.Code,
            Name = language.Name,
            NativeName = language.NativeName,
            FlagEmoji = language.FlagEmoji,
            IsActive = language.IsActive
        };
    }

    public async Task<LanguageResponse> UpdateLanguageAsync(Guid id, UpdateLanguageRequest request, CancellationToken cancellationToken = default)
    {
        var language = await _courseRepository.GetLanguageByIdAsync(id, cancellationToken);
        if (language == null)
        {
            throw new NotFoundException(Messages.Course.LanguageNotFound);
        }
        language.Name = request.Name.Trim();
        language.NativeName = request.NativeName.Trim();
        language.FlagEmoji = request.FlagEmoji.Trim();
        language.IsActive = request.IsActive;
        language.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateLanguageAsync(language, cancellationToken);

        return new LanguageResponse
        {
            Id = language.Id,
            Code = language.Code,
            Name = language.Name,
            NativeName = language.NativeName,
            FlagEmoji = language.FlagEmoji,
            IsActive = language.IsActive
        };
    }

    public async Task DeleteLanguageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var language = await _courseRepository.GetLanguageByIdAsync(id, cancellationToken);
        if (language == null)
        {
            throw new NotFoundException(Messages.Course.LanguageNotFound);
        }

        await _courseRepository.DeleteLanguageAsync(language, cancellationToken);
    }
}
