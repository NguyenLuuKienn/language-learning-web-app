using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using System.Text.Json;

namespace LanguageLearning.Application.Services;

public class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public CardService(
        ICardRepository cardRepository,
        IDeckRepository deckRepository,
        IReviewRepository reviewRepository,
        IStudyAccessService access,
        ICurrentUserService currentUser)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
        _reviewRepository = reviewRepository;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<List<CardResponse>> GetCardsByDeckIdAsync(Guid deckId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var deck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck == null)
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }
        if (!userId.HasValue || (!_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId.Value, deckId, cancellationToken)))
            throw new NotFoundException(Messages.Deck.DeckNotFound);

        var cards = await _cardRepository.GetByDeckIdAsync(deckId, cancellationToken);
        var responses = new List<CardResponse>();

        Dictionary<Guid, CardSchedule>? scheduleMap = null;
        if (userId.HasValue)
        {
            var schedules = await _reviewRepository.GetSchedulesByUserAsync(userId.Value, cancellationToken);
            scheduleMap = schedules.ToDictionary(s => s.CardId);
        }

        foreach (var c in cards)
        {
            CardScheduleResponse? scheduleResp = null;
            if (scheduleMap != null && scheduleMap.TryGetValue(c.Id, out var sch))
            {
                var state = SrsState.FromJson(sch.StateJson);
                scheduleResp = new CardScheduleResponse
                {
                    CardId = c.Id,
                    DueAt = sch.DueAt,
                    IntervalDays = state.IntervalDays,
                    EaseFactor = state.EaseFactor,
                    Repetitions = state.Repetitions,
                    Lapses = state.Lapses,
                    LastReviewedAt = sch.LastReviewedAt
                };
            }

            responses.Add(new CardResponse
            {
                Id = c.Id,
                DeckId = c.DeckId,
                Front = c.Front,
                Back = c.Back,
                Phonetic = c.Phonetic,
                PartOfSpeech = c.PartOfSpeech,
                Meanings = ReadMeanings(c),
                Example = c.Example,
                AudioUrl = c.AudioUrl,
                ImageUrl = c.ImageUrl,
                Version = c.Version,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Schedule = scheduleResp
            });
        }

        return responses;
    }

    public async Task<CardResponse> GetCardByIdAsync(Guid cardId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card == null)
        {
            throw new NotFoundException(Messages.Card.CardNotFound);
        }
        if (!userId.HasValue || (!_currentUser.IsAdmin() && !await _access.CanAccessCardAsync(userId.Value, cardId, cancellationToken)))
            throw new NotFoundException(Messages.Card.CardNotFound);

        CardScheduleResponse? scheduleResp = null;
        if (userId.HasValue)
        {
            var sch = await _reviewRepository.GetScheduleAsync(userId.Value, card.Id, cancellationToken);
            if (sch != null)
            {
                var state = SrsState.FromJson(sch.StateJson);
                scheduleResp = new CardScheduleResponse
                {
                    CardId = card.Id,
                    DueAt = sch.DueAt,
                    IntervalDays = state.IntervalDays,
                    EaseFactor = state.EaseFactor,
                    Repetitions = state.Repetitions,
                    Lapses = state.Lapses,
                    LastReviewedAt = sch.LastReviewedAt
                };
            }
        }

        return new CardResponse
        {
            Id = card.Id,
            DeckId = card.DeckId,
            Front = card.Front,
            Back = card.Back,
            Phonetic = card.Phonetic,
            PartOfSpeech = card.PartOfSpeech,
            Meanings = ReadMeanings(card),
            Example = card.Example,
            AudioUrl = card.AudioUrl,
            ImageUrl = card.ImageUrl,
            Version = card.Version,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt,
            Schedule = scheduleResp
        };
    }

    public async Task<CardResponse> CreateCardAsync(Guid deckId, CreateCardRequest request, Guid userId, CancellationToken cancellationToken = default, Guid? clientCardId = null)
    {
        var front = NormalizeCardText(request.Front, 1000, "Front");
        var back = NormalizeCardText(request.Back, 4000, "Back");
        var deck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck == null)
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }

        if (deck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException(Messages.Deck.NotDeckOwner);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.DeckNotFound);

        if (clientCardId.HasValue)
        {
            var existing = await _cardRepository.GetByIdAsync(clientCardId.Value, cancellationToken);
            if (existing != null)
            {
                if (existing.DeckId != deckId || existing.Front != front || existing.Back != back)
                    throw new BusinessRuleException("SYNC_CARD_CONFLICT", "Sync operation ID already belongs to a different card.");
                return await GetCardByIdAsync(existing.Id, userId, cancellationToken);
            }
        }

        var card = new Card
        {
            Id = clientCardId ?? Guid.NewGuid(),
            DeckId = deckId,
            Front = front,
            Back = back,
            Phonetic = request.Phonetic?.Trim() ?? string.Empty,
            PartOfSpeech = request.PartOfSpeech?.Trim() ?? string.Empty,
            MeaningsJson = NormalizeMeanings(request.Meanings, back),
            Example = request.Example?.Trim() ?? string.Empty,
            AudioUrl = request.AudioUrl,
            ImageUrl = request.ImageUrl,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _cardRepository.AddAsync(card, cancellationToken);

        deck.Version += 1;
        deck.UpdatedAt = DateTime.UtcNow;
        _deckRepository.Update(deck);
        await _deckRepository.SaveChangesAsync(cancellationToken);

        return new CardResponse
        {
            Id = card.Id,
            DeckId = card.DeckId,
            Front = card.Front,
            Back = card.Back,
            Phonetic = card.Phonetic,
            PartOfSpeech = card.PartOfSpeech,
            Meanings = ReadMeanings(card),
            Example = card.Example,
            AudioUrl = card.AudioUrl,
            ImageUrl = card.ImageUrl,
            Version = card.Version,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt
        };
    }

    public async Task<List<CardResponse>> CreateBatchCardsAsync(Guid deckId, BatchCreateCardsRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (request.Cards == null || request.Cards.Count is < 1 or > 500)
            throw new ArgumentException(Messages.Card.BatchSizeInvalid);
        var values = request.Cards.Select(item => item ?? throw new ArgumentException(Messages.Card.NullBatchItem))
            .Select(card => (
            Front: NormalizeCardText(card.Front, 1000, "Front"),
            Back: NormalizeCardText(card.Back, 4000, "Back"),
            card.AudioUrl,
            card.ImageUrl,
            Phonetic: card.Phonetic?.Trim() ?? string.Empty,
            PartOfSpeech: card.PartOfSpeech?.Trim() ?? string.Empty,
            MeaningsJson: NormalizeMeanings(card.Meanings, card.Back),
            Example: card.Example?.Trim() ?? string.Empty)).ToList();
        var deck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck == null)
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }

        if (deck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException(Messages.Deck.NotDeckOwner);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId, cancellationToken))
            throw new NotFoundException(Messages.Deck.DeckNotFound);

        var cards = values.Select(c => new Card
        {
            Id = Guid.NewGuid(),
            DeckId = deckId,
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

        await _cardRepository.AddBatchAsync(cards, cancellationToken);

        deck.Version += 1;
        deck.UpdatedAt = DateTime.UtcNow;
        _deckRepository.Update(deck);
        await _deckRepository.SaveChangesAsync(cancellationToken);

        return cards.Select(c => new CardResponse
        {
            Id = c.Id,
            DeckId = c.DeckId,
            Front = c.Front,
            Back = c.Back,
            Phonetic = c.Phonetic,
            PartOfSpeech = c.PartOfSpeech,
            Meanings = ReadMeanings(c),
            Example = c.Example,
            AudioUrl = c.AudioUrl,
            ImageUrl = c.ImageUrl,
            Version = c.Version,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    public async Task<CardResponse> UpdateCardAsync(Guid cardId, UpdateCardRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var front = NormalizeCardText(request.Front, 1000, "Front");
        var back = NormalizeCardText(request.Back, 4000, "Back");
        var card = await _cardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card == null)
        {
            throw new NotFoundException(Messages.Card.CardNotFound);
        }

        var deck = await _deckRepository.GetByIdAsync(card.DeckId, cancellationToken);
        if (deck != null && deck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException(Messages.Deck.NotDeckOwner);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessCardAsync(userId, cardId, cancellationToken))
            throw new NotFoundException(Messages.Card.CardNotFound);

        card.Front = front;
        if (request.Meanings != null || !string.Equals(card.Back, back, StringComparison.Ordinal))
            card.MeaningsJson = NormalizeMeanings(request.Meanings, back);
        card.Back = back;
        if (request.Phonetic != null) card.Phonetic = request.Phonetic.Trim();
        if (request.PartOfSpeech != null) card.PartOfSpeech = request.PartOfSpeech.Trim();
        if (request.Example != null) card.Example = request.Example.Trim();
        card.AudioUrl = request.AudioUrl;
        card.ImageUrl = request.ImageUrl;
        card.Version += 1;
        card.UpdatedAt = DateTime.UtcNow;

        await _cardRepository.UpdateAsync(card, cancellationToken);

        return new CardResponse
        {
            Id = card.Id,
            DeckId = card.DeckId,
            Front = card.Front,
            Back = card.Back,
            Phonetic = card.Phonetic,
            PartOfSpeech = card.PartOfSpeech,
            Meanings = ReadMeanings(card),
            Example = card.Example,
            AudioUrl = card.AudioUrl,
            ImageUrl = card.ImageUrl,
            Version = card.Version,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt
        };
    }

    public async Task DeleteCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card == null)
        {
            throw new NotFoundException(Messages.Card.CardNotFound);
        }

        var deck = await _deckRepository.GetByIdAsync(card.DeckId, cancellationToken);
        if (deck != null && deck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException(Messages.Deck.NotDeckOwner);
        }
        if (!_currentUser.IsAdmin() && !await _access.CanAccessCardAsync(userId, cardId, cancellationToken))
            throw new NotFoundException(Messages.Card.CardNotFound);

        await _cardRepository.DeleteAsync(card, cancellationToken);
    }

    private static string NormalizeCardText(string? text, int maxLength, string field)
    {
        var value = text?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.Length > maxLength)
            throw new ArgumentException(Messages.Course.LengthRange(field, maxLength));
        return value;
    }

    private static string NormalizeMeanings(List<string>? meanings, string fallback)
    {
        var values = meanings?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        if (values.Count == 0) values.Add(fallback);
        if (values.Count > 20 || values.Any(value => value.Length > 300))
            throw new ArgumentException(Messages.Course.LengthRange("Meanings", 300));
        return JsonSerializer.Serialize(values);
    }

    private static List<string> ReadMeanings(Card card) =>
        JsonSerializer.Deserialize<List<string>>(card.MeaningsJson) is { Count: > 0 } meanings ? meanings : [card.Back];
}
