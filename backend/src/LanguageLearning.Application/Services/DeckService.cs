
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
namespace LanguageLearning.Application.Services;

public class DeckService : IDeckService
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStudyAccessService _access;

    public DeckService(
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService,
        IStudyAccessService access)
    {
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _access = access;
    }

    public async Task<DeckResponse> CreateDeckAsync(
        CreateDeckRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var target = await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUserService.IsAdmin() && target?.LanguageId != request.LanguageId)
            throw new ArgumentException(Messages.Course.StudyTargetRequired);

        if (request.LanguageId == Guid.Empty ||
            !await _deckRepository.LanguageExistsAsync(
                request.LanguageId, cancellationToken))
        {
            throw new ArgumentException(Messages.Deck.InvalidLanguage);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException(Messages.Deck.TitleRequired);

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            LanguageId = request.LanguageId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsPublic = request.IsPublic,
            Version = 1
        };

        foreach (var tag in request.Tags ?? new List<string>())
        {
            var normalizedTag = tag?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedTag))
                continue;
            if (!_currentUserService.IsAdmin() && normalizedTag.StartsWith("course-level:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (deck.Tags.Any(t => t.Tag == normalizedTag))
                continue;

            deck.Tags.Add(new DeckTag
            {
                Id = Guid.NewGuid(),
                Tag = normalizedTag
            });
        }
        if (!_currentUserService.IsAdmin() && target != null)
            deck.Tags.Add(new DeckTag { Id = Guid.NewGuid(), Tag = $"course-level:{target.Level}" });

        await _deckRepository.AddAsync(
            deck, cancellationToken);

        await _deckRepository.SaveChangesAsync(
            cancellationToken);

        return new DeckResponse
        {
            Id = deck.Id,
            OwnerId = deck.OwnerId,
            LanguageId = deck.LanguageId,
            Title = deck.Title,
            Description = deck.Description,
            IsPublic = deck.IsPublic,
            Version = deck.Version,
            Tags = deck.Tags.Select(t => t.Tag).ToList(),
            TotalCards = 0,
            NewCards = 0,
            DueCards = 0,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt
        };
    }

    public async Task<PagedResponse<DeckResponse>> GetDecksAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();

        ArgumentNullException.ThrowIfNull(pagination);
        var target = _currentUserService.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUserService.IsAdmin() && target == null)
            return new PagedResponse<DeckResponse>([], pagination.Page, pagination.PageSize, 0);

        var (decks, totalItems) =
            await _deckRepository.GetAccessibleDecksAsync(
                userId,
                pagination.Page,
                pagination.PageSize,
                cancellationToken,
                target?.LanguageId,
                target?.Level);

        var deckIds = decks
            .Select(deck => deck.Id)
            .ToArray();

        var statistics =
            await _deckRepository.GetDeckStatisticsBatchAsync(
                deckIds,
                userId,
                cancellationToken);

        var items = new List<DeckResponse>();

        foreach (var deck in decks)
        {
            var stats = statistics.TryGetValue(
                deck.Id, out var deckStats)
                ? deckStats
                : (TotalCards: 0, NewCards: 0, DueCards: 0);

            items.Add(new DeckResponse
            {
                Id = deck.Id,
                OwnerId = deck.OwnerId,
                LanguageId = deck.LanguageId,
                LanguageCode = deck.Language?.Code ?? string.Empty,
                Title = deck.Title,
                Description = deck.Description,
                IsPublic = deck.IsPublic,
                Version = deck.Version,
                Tags = deck.Tags
                    .Select(tag => tag.Tag)
                    .ToList(),
                TotalCards = stats.TotalCards,
                NewCards = stats.NewCards,
                DueCards = stats.DueCards,
                CreatedAt = deck.CreatedAt,
                UpdatedAt = deck.UpdatedAt
            });
        }

        return new PagedResponse<DeckResponse>(
            items,
            pagination.Page,
            pagination.PageSize,
            totalItems);
    }

    public async Task<DeckResponse> GetDeckByIdAsync(
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var deck = await _deckRepository.GetByIdAsync(
            deckId, cancellationToken);

        if (deck == null || (deck.OwnerId != userId && !deck.IsPublic))
        {
            throw new NotFoundException(
                Messages.Deck.NotAccessible);
        }
        if (!_currentUserService.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.NotAccessible);

        var stats = await _deckRepository.GetDeckStatisticsAsync(
            deckId,
            userId,
            cancellationToken);

        return new DeckResponse
        {
            Id = deck.Id,
            OwnerId = deck.OwnerId,
            LanguageId = deck.LanguageId,
            Title = deck.Title,
            Description = deck.Description,
            IsPublic = deck.IsPublic,
            Version = deck.Version,
            Tags = deck.Tags
                .Select(tag => tag.Tag)
                .ToList(),
            TotalCards = stats.TotalCards,
            NewCards = stats.NewCards,
            DueCards = stats.DueCards,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt
        };
    }

    public async Task<DeckResponse> UpdateDeckAsync(
        Guid deckId,
        UpdateDeckRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var deck = await _deckRepository.GetByIdAsync(
            deckId, cancellationToken);

        if (deck == null || deck.OwnerId != userId)
        {
            throw new NotFoundException(
                Messages.Deck.NotAccessible);
        }
        if (!_currentUserService.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.NotAccessible);

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException(
                    Messages.Deck.TitleEmpty);

            deck.Title = request.Title.Trim();
        }

        if (request.Description != null)
            deck.Description = request.Description.Trim();

        if (request.LanguageId.HasValue)
        {
            var target = await _access.GetTargetAsync(userId, cancellationToken);
            if (!_currentUserService.IsAdmin() && target?.LanguageId != request.LanguageId.Value)
                throw new ArgumentException(Messages.Course.DeckLanguageMismatch);
            if (request.LanguageId.Value == Guid.Empty ||
                !await _deckRepository.LanguageExistsAsync(
                    request.LanguageId.Value,
                    cancellationToken))
            {
                throw new ArgumentException(
                    Messages.Deck.InvalidLanguage);
            }

            deck.LanguageId = request.LanguageId.Value;
        }

        if (request.IsPublic.HasValue)
            deck.IsPublic = request.IsPublic.Value;

        if (request.Tags != null)
        {
            var newTags = request.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct()
                .ToList();
            if (!_currentUserService.IsAdmin())
            {
                var target = await _access.GetTargetAsync(userId, cancellationToken);
                newTags.RemoveAll(tag => tag.StartsWith("course-level:", StringComparison.OrdinalIgnoreCase));
                if (target != null) newTags.Add($"course-level:{target.Level}");
            }

            var tagsToRemove = deck.Tags
                .Where(tag => !newTags.Contains(tag.Tag))
                .ToList();

            foreach (var tag in tagsToRemove)
            {
                deck.Tags.Remove(tag);
            }

            foreach (var tag in newTags)
            {
                if (deck.Tags.Any(t => t.Tag == tag))
                    continue;

                deck.Tags.Add(new DeckTag
                {
                    Id = Guid.NewGuid(),
                    Tag = tag
                });
            }
        }

        deck.Version++;

        await _deckRepository.SaveChangesAsync(
            cancellationToken);

        var stats = await _deckRepository.GetDeckStatisticsAsync(
            deck.Id,
            userId,
            cancellationToken);

        return new DeckResponse
        {
            Id = deck.Id,
            OwnerId = deck.OwnerId,
            LanguageId = deck.LanguageId,
            Title = deck.Title,
            Description = deck.Description,
            IsPublic = deck.IsPublic,
            Version = deck.Version,
            Tags = deck.Tags
                .Select(t => t.Tag)
                .ToList(),
            TotalCards = stats.TotalCards,
            NewCards = stats.NewCards,
            DueCards = stats.DueCards,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt
        };
    }

    public async Task DeleteDeckAsync(
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var deck = await _deckRepository.GetByIdAsync(
            deckId, cancellationToken);

        if (deck == null || deck.OwnerId != userId)
        {
            throw new NotFoundException(
                Messages.Deck.NotAccessible);
        }
        if (!_currentUserService.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.NotAccessible);

        _deckRepository.Delete(deck);

        await _deckRepository.SaveChangesAsync(
            cancellationToken);
    }
}
