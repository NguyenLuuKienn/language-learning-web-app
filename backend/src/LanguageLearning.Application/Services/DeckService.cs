
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

    // Create a new deck
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

        // Add tags
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

    // Get decks accessible to the user
    public async Task<List<DeckResponse>> GetDecksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        var decks = await _deckRepository.GetAccessibleDecksAsync(
            userId, cancellationToken);

        var result = new List<DeckResponse>();

        foreach (var deck in decks)
        {
            // Get statistics for each deck
            var stats = await _deckRepository.GetDeckStatisticsAsync(
                deck.Id,
                userId,
                cancellationToken);

            result.Add(new DeckResponse
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

        return result;
    }

    // Get deck details by ID
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

        // Check access permission
        if (deck.OwnerId != userId && !deck.IsPublic)
            return null;

        // Get card statistics for the current user
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

    // Update an existing deck
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

        // Only the owner can update a deck
        if (deck == null || deck.OwnerId != userId)
            return null;

        // Update title
        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException(
                    "Deck title cannot be empty.");

            deck.Title = request.Title.Trim();
        }

        // Update description
        if (request.Description != null)
            deck.Description = request.Description.Trim();

        // Update language
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

        // Update visibility
        if (request.IsPublic.HasValue)
            deck.IsPublic = request.IsPublic.Value;

        // Update tags
        if (request.Tags != null)
        {
            var newTags = request.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct()
                .ToList();

            // Remove tags that are no longer needed
            var tagsToRemove = deck.Tags
                .Where(tag => !newTags.Contains(tag.Tag))
                .ToList();

            foreach (var tag in tagsToRemove)
            {
                deck.Tags.Remove(tag);
            }

            // Add tags that do not exist
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

        await _deckRepository.SaveChangesAsync(
            cancellationToken);

        // Get updated card statistics
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

    // Delete a deck owned by the user
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

        // Only the owner can delete a deck
        if (deck == null || deck.OwnerId != userId)
            return false;

        _deckRepository.Delete(deck);

        await _deckRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
