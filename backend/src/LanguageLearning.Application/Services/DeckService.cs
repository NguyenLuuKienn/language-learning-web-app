
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public class DeckService : IDeckService
{
    private readonly IDeckRepository _deckRepository;

    public DeckService(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<DeckResponse> CreateDeckAsync(
        CreateDeckRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        if (request.LanguageId == Guid.Empty ||
            !await _deckRepository.LanguageExistsAsync(
                request.LanguageId, cancellationToken))
        {
            throw new ArgumentException("Invalid language.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Deck title is required.");

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

            if (deck.Tags.Any(t => t.Tag == normalizedTag))
                continue;

            deck.Tags.Add(new DeckTag
            {
                Id = Guid.NewGuid(),
                Tag = normalizedTag
            });
        }

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

    public async Task<PagedDeckResponse> GetDecksAsync(
        Guid userId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 10;

        if (pageSize > 50)
            pageSize = 50;

        var (decks, totalItems) =
            await _deckRepository.GetAccessibleDecksAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

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

        var totalPages = (int)Math.Ceiling(
            (double)totalItems / pageSize);

        return new PagedDeckResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1 && totalPages > 0
        };
    }

    public async Task<DeckResponse?> GetDeckByIdAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        var deck = await _deckRepository.GetByIdAsync(
            deckId, cancellationToken);

        if (deck == null)
            return null;

        if (deck.OwnerId != userId && !deck.IsPublic)
            return null;

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

    public async Task<DeckResponse?> UpdateDeckAsync(
        Guid deckId,
        UpdateDeckRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        var deck = await _deckRepository.GetByIdAsync(
            deckId, cancellationToken);

        if (deck == null || deck.OwnerId != userId)
            return null;

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException(
                    "Deck title cannot be empty.");

            deck.Title = request.Title.Trim();
        }

        if (request.Description != null)
            deck.Description = request.Description.Trim();

        if (request.LanguageId.HasValue)
        {
            if (request.LanguageId.Value == Guid.Empty ||
                !await _deckRepository.LanguageExistsAsync(
                    request.LanguageId.Value,
                    cancellationToken))
            {
                throw new ArgumentException(
                    "Invalid language.");
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

    public async Task<bool> DeleteDeckAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        var deck = await _deckRepository.GetByIdAsync(
            deckId, cancellationToken);

        if (deck == null || deck.OwnerId != userId)
            return false;

        _deckRepository.Delete(deck);

        await _deckRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
