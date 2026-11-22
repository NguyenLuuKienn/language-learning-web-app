using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using System.Text.Json;

namespace LanguageLearning.Application.Services;

public class VocabularyService : IVocabularyService
{
    private readonly IVocabularyRepository _vocabularyRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public VocabularyService(IVocabularyRepository vocabularyRepository, IStudyAccessService access, ICurrentUserService currentUser)
    {
        _vocabularyRepository = vocabularyRepository;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<List<VocabularyResponse>> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin() && !await _access.CanAccessLessonAsync(_currentUser.GetRequiredUserId(), lessonId, cancellationToken))
            throw new NotFoundException(Messages.Course.LessonNotFound);
        var list = await _vocabularyRepository.GetByLessonIdAsync(lessonId, cancellationToken);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<List<VocabularyResponse>> SearchAsync(Guid? languageId, string? term, CancellationToken cancellationToken = default)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(_currentUser.GetRequiredUserId(), cancellationToken);
        if (!_currentUser.IsAdmin() && target == null) return [];
        if (target != null && languageId.HasValue && languageId != target.LanguageId) return [];
        var domains = target?.Domains?.Count > 0 ? target.Domains : target == null ? null : [target.Domain];
        var list = await _vocabularyRepository.SearchAsync(target?.LanguageId ?? languageId, term, 50, cancellationToken, target?.Level, domains);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<VocabularyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vocab = await _vocabularyRepository.GetByIdAsync(id, cancellationToken);
        if (vocab == null)
        {
            throw new NotFoundException(Messages.Course.VocabularyNotFound);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessVocabularyAsync(_currentUser.GetRequiredUserId(), id, cancellationToken))
            throw new NotFoundException(Messages.Course.VocabularyNotFound);

        return MapToResponse(vocab);
    }

    public async Task<VocabularyResponse> CreateAsync(CreateVocabularyRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LanguageId == Guid.Empty || !request.LessonId.HasValue || request.LessonId == Guid.Empty)
            throw new ArgumentException(Messages.Course.LanguageAndLessonRequired);
        var lessonLanguageId = await _vocabularyRepository.GetLessonLanguageIdAsync(request.LessonId.Value, cancellationToken);
        if (lessonLanguageId == null || lessonLanguageId != request.LanguageId)
            throw new ArgumentException(Messages.Course.LessonLanguageMismatch);
        var term = ValidateVocabularyText(request.Term, 100, "Term");
        var meaning = ValidateVocabularyText(request.Meaning, 300, "Meaning");
        var vocab = new Vocabulary
        {
            Id = Guid.NewGuid(),
            LessonId = request.LessonId.Value,
            LanguageId = request.LanguageId,
            Term = term,
            Phonetic = request.Phonetic?.Trim() ?? string.Empty,
            Meaning = meaning,
            MeaningsJson = SerializeMeanings(request.Meanings, meaning),
            PartOfSpeech = request.PartOfSpeech?.Trim() ?? string.Empty,
            Example = request.Example?.Trim() ?? string.Empty,
            AudioUrl = request.AudioUrl,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _vocabularyRepository.AddAsync(vocab, cancellationToken);
        return MapToResponse(vocab);
    }

    public async Task<VocabularyResponse> UpdateAsync(Guid id, UpdateVocabularyRequest request, CancellationToken cancellationToken = default)
    {
        var term = ValidateVocabularyText(request.Term, 100, "Term");
        var meaning = ValidateVocabularyText(request.Meaning, 300, "Meaning");
        var vocab = await _vocabularyRepository.GetByIdAsync(id, cancellationToken);
        if (vocab == null)
        {
            throw new NotFoundException(Messages.Course.VocabularyNotFound);
        }

        vocab.Term = term;
        vocab.Phonetic = request.Phonetic?.Trim() ?? string.Empty;
        var meaningChanged = !string.Equals(vocab.Meaning, meaning, StringComparison.Ordinal);
        vocab.MeaningsJson = request.Meanings == null && !meaningChanged
            ? vocab.MeaningsJson
            : SerializeMeanings(request.Meanings, meaning);
        vocab.Meaning = meaning;
        if (request.PartOfSpeech != null) vocab.PartOfSpeech = request.PartOfSpeech.Trim();
        vocab.Example = request.Example?.Trim() ?? string.Empty;
        vocab.AudioUrl = request.AudioUrl;
        vocab.ImageUrl = request.ImageUrl;
        vocab.UpdatedAt = DateTime.UtcNow;

        await _vocabularyRepository.UpdateAsync(vocab, cancellationToken);
        return MapToResponse(vocab);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vocab = await _vocabularyRepository.GetByIdAsync(id, cancellationToken);
        if (vocab == null)
        {
            throw new NotFoundException(Messages.Course.VocabularyNotFound);
        }

        await _vocabularyRepository.DeleteAsync(vocab, cancellationToken);
    }

    private static string ValidateVocabularyText(string? text, int maxLength, string field)
    {
        var value = text?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.Length > maxLength)
            throw new ArgumentException(Messages.Course.LengthRange(field, maxLength));
        return value;
    }

    private static string SerializeMeanings(List<string>? meanings, string fallback)
    {
        var values = meanings?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        if (values.Count == 0) values.Add(fallback);
        if (values.Count > 20 || values.Any(value => value.Length > 300))
            throw new ArgumentException(Messages.Course.LengthRange("Meanings", 300));
        return JsonSerializer.Serialize(values);
    }

    private static VocabularyResponse MapToResponse(Vocabulary v) => new()
    {
        Id = v.Id,
        LessonId = v.LessonId,
        LanguageId = v.LanguageId,
        Term = v.Term,
        Phonetic = v.Phonetic,
        Meaning = v.Meaning,
        Meanings = JsonSerializer.Deserialize<List<string>>(v.MeaningsJson) is { Count: > 0 } meanings ? meanings : [v.Meaning],
        PartOfSpeech = v.PartOfSpeech,
        Example = v.Example,
        AudioUrl = v.AudioUrl,
        ImageUrl = v.ImageUrl
    };
}
