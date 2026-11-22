using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public class SrsService : ISrsService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISrsScheduler _srsScheduler;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public SrsService(
        IReviewRepository reviewRepository,
        ICardRepository cardRepository,
        IUserRepository userRepository,
        ISrsScheduler srsScheduler,
        IStudyAccessService access,
        ICurrentUserService currentUser)
    {
        _reviewRepository = reviewRepository;
        _cardRepository = cardRepository;
        _userRepository = userRepository;
        _srsScheduler = srsScheduler;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<ReviewOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default, Guid? deckId = null, Guid? lessonId = null)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null) return new ReviewOverviewResponse();
        if (deckId.HasValue && !_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId.Value, cancellationToken))
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        return await _reviewRepository.GetOverviewAsync(userId, cancellationToken, target?.LanguageId, target?.Level, deckId, lessonId);
    }

    public async Task<List<ReviewLessonSourceResponse>> GetLessonSourcesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null) return [];
        return await _reviewRepository.GetLessonSourcesAsync(userId, target?.LanguageId, target?.Level, cancellationToken);
    }

    public async Task<DueCardsSummaryResponse> GetDueCardsAsync(Guid userId, Guid? deckId, int limit = 50, CancellationToken cancellationToken = default, bool dueOnly = false, bool newOnly = false, Guid? lessonId = null)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null)
            return new DueCardsSummaryResponse { Cards = [] };
        if (deckId.HasValue && !_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId, deckId.Value, cancellationToken))
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        if (dueOnly && newOnly)
            throw new BusinessRuleException("SRS_QUEUE_CONFLICT", "Choose either due cards or new cards.");
        var dueCards = await _reviewRepository.GetDueCardsAsync(userId, deckId, limit, cancellationToken, target?.LanguageId, target?.Level, dueOnly, newOnly, lessonId);
        var schedules = await _reviewRepository.GetSchedulesForCardsAsync(userId, dueCards.Select(card => card.Id).ToArray(), cancellationToken);
        var scheduleMap = schedules.ToDictionary(s => s.CardId);

        var cardResponses = new List<CardResponse>();
        int dueCount = 0;
        int newCount = 0;

        foreach (var c in dueCards)
        {
            CardScheduleResponse? schResp = null;
            if (scheduleMap.TryGetValue(c.Id, out var sch))
            {
                if (sch.DueAt <= DateTime.UtcNow) dueCount++;
                var state = SrsState.FromJson(sch.StateJson);
                schResp = new CardScheduleResponse
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
            else
            {
                newCount++;
            }

            cardResponses.Add(new CardResponse
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
                UpdatedAt = c.UpdatedAt,
                Schedule = schResp
            });
        }

        return new DueCardsSummaryResponse
        {
            DueCount = dueCount,
            NewCount = newCount,
            Cards = cardResponses
        };
    }

    public async Task<ReviewResultResponse> SubmitReviewAsync(Guid userId, SubmitReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin() && !await _access.CanAccessCardAsync(userId, request.CardId, cancellationToken))
            throw new NotFoundException(Messages.Srs.CardToReviewNotFound);
        var existingEvent = await _reviewRepository.GetExistingReviewEventAsync(userId, request.ClientEventId, cancellationToken);
        if (existingEvent != null)
        {
            if (existingEvent.CardId != request.CardId || existingEvent.Rating != request.Rating)
                throw new BusinessRuleException("SRS_EVENT_CONFLICT", "This review request identifier was already used for a different answer.");
            var existingSchedule = await _reviewRepository.GetScheduleAsync(userId, request.CardId, cancellationToken);
            var existingState = SrsState.FromJson(existingSchedule?.StateJson);

            return new ReviewResultResponse
            {
                EventId = existingEvent.Id,
                ClientEventId = existingEvent.ClientEventId,
                CardId = existingEvent.CardId,
                Rating = existingEvent.Rating,
                DueAt = existingSchedule?.DueAt ?? DateTime.UtcNow,
                NextIntervalDays = existingState.IntervalDays,
                IsDuplicate = true
            };
        }

        var card = await _cardRepository.GetByIdAsync(request.CardId, cancellationToken);
        if (card == null)
        {
            throw new NotFoundException(Messages.Srs.CardToReviewNotFound);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var timezone = user?.Timezone ?? "Asia/Ho_Chi_Minh";

        var reviewedAt = request.ReviewedAt ?? DateTime.UtcNow;

        var schedule = await _reviewRepository.GetScheduleAsync(userId, request.CardId, cancellationToken);
        var currentState = schedule != null ? SrsState.FromJson(schedule.StateJson) : new SrsState();
        var wasDue = schedule == null ? (bool?)null : schedule.DueAt <= reviewedAt;
        var previousIntervalDays = schedule == null ? (double?)null : currentState.IntervalDays;

        var nextState = _srsScheduler.CalculateNext(currentState, request.Rating, reviewedAt);

        if (schedule == null)
        {
            schedule = new CardSchedule
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = request.CardId,
                DueAt = nextState.DueAt,
                StateJson = nextState.ToJson(),
                AlgorithmVersion = _srsScheduler.Version,
                LastReviewedAt = reviewedAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            schedule.DueAt = nextState.DueAt;
            schedule.StateJson = nextState.ToJson();
            schedule.AlgorithmVersion = _srsScheduler.Version;
            schedule.LastReviewedAt = reviewedAt;
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        var reviewEvent = new ReviewEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = request.CardId,
            Rating = request.Rating,
            ReviewedAt = reviewedAt,
            ClientEventId = request.ClientEventId,
            WasDue = wasDue,
            PreviousIntervalDays = previousIntervalDays,
            AlgorithmVersion = _srsScheduler.Version,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!await _reviewRepository.CommitReviewAsync(schedule, reviewEvent, timezone, cancellationToken))
        {
            var savedEvent = await _reviewRepository.GetExistingReviewEventAsync(userId, request.ClientEventId, cancellationToken);
            if (savedEvent == null || savedEvent.CardId != request.CardId || savedEvent.Rating != request.Rating)
                throw new BusinessRuleException("SRS_EVENT_CONFLICT", "This review request identifier was already used for a different answer.");
            var savedSchedule = await _reviewRepository.GetScheduleAsync(userId, request.CardId, cancellationToken);
            var savedState = SrsState.FromJson(savedSchedule?.StateJson);
            return new ReviewResultResponse
            {
                EventId = savedEvent.Id, ClientEventId = savedEvent.ClientEventId,
                CardId = savedEvent.CardId, Rating = savedEvent.Rating,
                DueAt = savedSchedule?.DueAt ?? DateTime.UtcNow,
                NextIntervalDays = savedState.IntervalDays, IsDuplicate = true
            };
        }

        return new ReviewResultResponse
        {
            EventId = reviewEvent.Id,
            ClientEventId = reviewEvent.ClientEventId,
            CardId = reviewEvent.CardId,
            Rating = reviewEvent.Rating,
            DueAt = schedule.DueAt,
            NextIntervalDays = nextState.IntervalDays,
            IsDuplicate = false
        };
    }

    public async Task<List<ReviewResultResponse>> GetHistoryAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null) return [];
        var events = await _reviewRepository.GetReviewHistoryAsync(userId, limit, cancellationToken, target?.LanguageId, target?.Level);
        var schedules = await _reviewRepository.GetSchedulesByUserAsync(userId, cancellationToken);
        var scheduleMap = schedules.ToDictionary(s => s.CardId);

        return events.Select(e =>
        {
            double interval = 0;
            DateTime due = DateTime.UtcNow;
            if (scheduleMap.TryGetValue(e.CardId, out var sch))
            {
                var st = SrsState.FromJson(sch.StateJson);
                interval = st.IntervalDays;
                due = sch.DueAt;
            }

            return new ReviewResultResponse
            {
                EventId = e.Id,
                ClientEventId = e.ClientEventId,
                CardId = e.CardId,
                Rating = e.Rating,
                DueAt = due,
                NextIntervalDays = interval,
                IsDuplicate = false
            };
        }).ToList();
    }
}
