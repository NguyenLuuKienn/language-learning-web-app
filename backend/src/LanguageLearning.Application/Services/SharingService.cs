using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Sharing;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class SharingService : ISharingService
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public SharingService(IDeckRepository deckRepository, ICardRepository cardRepository, IStudyAccessService access, ICurrentUserService currentUser)
    {
        _deckRepository = deckRepository;
        _cardRepository = cardRepository;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<List<PublicDeckResponse>> GetPublicDecksAsync(Guid? languageId, string? search, CancellationToken cancellationToken = default)
    {
        if (_currentUser.IsAdmin())
            return await _deckRepository.GetDeckSummariesAsync(languageId, search, false, cancellationToken);
        var target = await _access.GetTargetAsync(_currentUser.GetRequiredUserId(), cancellationToken);
        if (target == null || (languageId.HasValue && languageId != target.LanguageId)) return [];
        return await _deckRepository.GetDeckSummariesAsync(target.LanguageId, search, false, cancellationToken, target.Level);
    }

    public async Task<PublicDeckPreviewResponse> GetPublicDeckPreviewAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        var deck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck == null || !IsCommunityDeck(deck))
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(_currentUser.GetRequiredUserId(), deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.DeckNotFound);

        var cards = await _cardRepository.GetSampleByDeckIdAsync(deckId, 10, cancellationToken);
        var totalCards = await _cardRepository.CountByDeckIdAsync(deckId, cancellationToken);
        var sampleCards = cards.Select(c => new CardResponse
        {
            Id = c.Id,
            DeckId = c.DeckId,
            Front = c.Front,
            Back = c.Back,
            Phonetic = c.Phonetic,
            PartOfSpeech = c.PartOfSpeech,
            Meanings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(c.MeaningsJson) is { Count: > 0 } meanings ? meanings : [c.Back],
            Example = c.Example,
            AudioUrl = c.AudioUrl,
            ImageUrl = c.ImageUrl,
            Version = c.Version,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return new PublicDeckPreviewResponse
        {
            Id = deck.Id,
            Title = deck.Title,
            Description = deck.Description,
            LanguageName = deck.Language?.Name ?? string.Empty,
            OwnerDisplayName = deck.Owner?.DisplayName ?? "Community Member",
            TotalCards = totalCards,
            SampleCards = sampleCards
        };
    }

    public async Task<CloneDeckResponse> CloneDeckAsync(Guid deckId, Guid userId, CancellationToken cancellationToken = default)
    {
        var originalDeck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (originalDeck == null || !IsCommunityDeck(originalDeck))
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.DeckNotFound);

        var clonedDeck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            LanguageId = originalDeck.LanguageId,
            Title = $"{originalDeck.Title} (Cloned)",
            Description = originalDeck.Description,
            IsPublic = false,
            SourceDeckId = originalDeck.Id,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var tag in originalDeck.Tags)
        {
            clonedDeck.Tags.Add(new DeckTag
            {
                Id = Guid.NewGuid(),
                DeckId = clonedDeck.Id,
                Tag = tag.Tag
            });
        }

        await _deckRepository.AddAsync(clonedDeck, cancellationToken);

        var originalCards = await _cardRepository.GetByDeckIdAsync(deckId, cancellationToken);
        var clonedCards = originalCards.Select(c => new Card
        {
            Id = Guid.NewGuid(),
            DeckId = clonedDeck.Id,
            Front = c.Front,
            Back = c.Back,
            Phonetic = c.Phonetic,
            PartOfSpeech = c.PartOfSpeech,
            MeaningsJson = c.MeaningsJson,
            Example = c.Example,
            AudioUrl = c.AudioUrl,
            ImageUrl = c.ImageUrl,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        if (clonedCards.Any())
        {
            await _cardRepository.AddBatchAsync(clonedCards, cancellationToken);
        }

        return new CloneDeckResponse
        {
            ClonedDeckId = clonedDeck.Id,
            SourceDeckId = originalDeck.Id,
            Title = clonedDeck.Title,
            ClonedCardsCount = clonedCards.Count
        };
    }

    private static bool IsCommunityDeck(Deck deck) => deck.IsPublic && !deck.IsDeleted &&
        (deck.Owner?.Role == UserRole.Learner ||
         !deck.Tags.Any(tag => tag.Tag.StartsWith("course-level:", StringComparison.OrdinalIgnoreCase)) ||
         deck.Tags.Any(tag => tag.Tag.StartsWith("vocab-lesson:", StringComparison.OrdinalIgnoreCase)));
}
