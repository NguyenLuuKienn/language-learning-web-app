using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.DTOs.Sync;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;

public class SyncService : ISyncService
{
    private readonly ISyncRepository _syncRepository;
    private readonly ISrsService _srsService;
    private readonly ICardService _cardService;
    private readonly IDeckRepository _deckRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public SyncService(
        ISyncRepository syncRepository,
        ISrsService srsService,
        ICardService cardService,
        IDeckRepository deckRepository,
        ICardRepository cardRepository,
        IStudyAccessService access,
        ICurrentUserService currentUser)
    {
        _syncRepository = syncRepository;
        _srsService = srsService;
        _cardService = cardService;
        _deckRepository = deckRepository;
        _cardRepository = cardRepository;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<SyncPushResponse> PushOperationsAsync(Guid userId, SyncPushRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Operations == null || request.Operations.Count > 500)
            throw new ArgumentException(Messages.Sync.BatchTooLarge);
        var results = new List<SyncOperationResult>();

        foreach (var op in request.Operations)
        {
            if (op == null || op.OperationId == Guid.Empty || op.EntityId == Guid.Empty
                || op.Type is not ("review.submit" or "card.create"))
            {
                results.Add(new SyncOperationResult
                {
                    OperationId = op?.OperationId ?? Guid.Empty,
                    Status = "Rejected",
                    Message = Messages.Sync.InvalidOperation
                });
                continue;
            }
            if (await _syncRepository.IsOperationProcessedAsync(userId, op.OperationId, cancellationToken))
            {
                results.Add(new SyncOperationResult
                {
                    OperationId = op.OperationId,
                    Status = "Duplicate",
                    Message = Messages.Sync.OperationDuplicate
                });
                continue;
            }

            try
            {
                var payloadRaw = op.Payload.GetRawText();
                var payloadHash = ComputeHash(payloadRaw);

                if (string.Equals(op.Type, "review.submit", StringComparison.OrdinalIgnoreCase))
                {
                    var ratingStr = op.Payload.TryGetProperty("rating", out var rProp) ? rProp.GetString() : "Good";
                    if (!Enum.TryParse<SrsRating>(ratingStr, true, out var rating) || !Enum.IsDefined(rating))
                        throw new ArgumentException(Messages.Srs.InvalidRating);

                    await _srsService.SubmitReviewAsync(userId, new SubmitReviewRequest
                    {
                        ClientEventId = op.OperationId,
                        CardId = op.EntityId,
                        Rating = rating,
                        ReviewedAt = op.OccurredAt
                    }, cancellationToken);

                    await RecordLedgerAsync(userId, op.OperationId, op.Type, op.EntityId, payloadHash, "Accepted", cancellationToken);

                    results.Add(new SyncOperationResult
                    {
                        OperationId = op.OperationId,
                        Status = "Accepted",
                        Message = Messages.Sync.OperationAccepted
                    });
                }
                else if (string.Equals(op.Type, "card.create", StringComparison.OrdinalIgnoreCase))
                {
                    var front = op.Payload.TryGetProperty("front", out var f) ? f.GetString() ?? string.Empty : string.Empty;
                    var back = op.Payload.TryGetProperty("back", out var b) ? b.GetString() ?? string.Empty : string.Empty;
                    var deckId = op.Payload.TryGetProperty("deckId", out var d) && Guid.TryParse(d.GetString(), out var dId) ? dId : op.EntityId;

                    await _cardService.CreateCardAsync(deckId, new CreateCardRequest
                    {
                        Front = front,
                        Back = back
                    }, userId, cancellationToken, op.OperationId);

                    await RecordLedgerAsync(userId, op.OperationId, op.Type, op.EntityId, payloadHash, "Accepted", cancellationToken);

                    results.Add(new SyncOperationResult
                    {
                        OperationId = op.OperationId,
                        Status = "Accepted",
                        Message = Messages.Sync.OperationAccepted
                    });
                }
                else
                {
                    results.Add(new SyncOperationResult
                    {
                        OperationId = op.OperationId,
                        Status = "Rejected",
                        Message = Messages.Sync.UnsupportedOperation
                    });
                }
            }
            catch (Exception ex) when (ex is ArgumentException or NotFoundException or UnauthorizedAccessException or BusinessRuleException)
            {
                results.Add(new SyncOperationResult
                {
                    OperationId = op.OperationId,
                    Status = "Rejected",
                    Message = ex.Message
                });
            }
            catch (Exception)
            {
                results.Add(new SyncOperationResult
                {
                    OperationId = op.OperationId,
                    Status = "Rejected",
                    Message = Messages.Sync.OperationRejected
                });
            }
        }

        return new SyncPushResponse
        {
            ProcessedCount = results.Count,
            Results = results
        };
    }

    public async Task<SyncPullResponse> PullChangesAsync(Guid userId, DateTime? cursor, CancellationToken cancellationToken = default)
    {
        var since = cursor ?? DateTime.MinValue;
        var target = _currentUser.IsAdmin() ? null : await _access.GetTargetAsync(userId, cancellationToken);
        if (!_currentUser.IsAdmin() && target == null)
            return new SyncPullResponse { ServerTimestamp = DateTime.UtcNow };
        var changedDecks = await _deckRepository.GetChangedUserDecksAsync(userId, since, cancellationToken, target?.LanguageId, target?.Level);
        var userDecks = changedDecks
            .Select(d => new DeckResponse
            {
                Id = d.Id,
                LanguageId = d.LanguageId,
                Title = d.Title,
                Description = d.Description,
                IsPublic = d.IsPublic,
                Version = d.Version,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                Tags = d.Tags.Select(t => t.Tag).ToList()
            }).ToList();

        var changedCards = await _deckRepository.GetChangedUserCardsAsync(userId, since, cancellationToken, target?.LanguageId, target?.Level);
        var cards = changedCards
            .Select(c => new CardResponse
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

        return new SyncPullResponse
        {
            ServerTimestamp = DateTime.UtcNow,
            UpdatedDecks = userDecks,
            UpdatedCards = cards
        };
    }

    private async Task RecordLedgerAsync(Guid userId, Guid opId, string type, Guid entityId, string hash, string status, CancellationToken cancellationToken)
    {
        await _syncRepository.RecordOperationAsync(new SyncOperation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClientOperationId = opId,
            OperationType = type,
            EntityId = entityId,
            PayloadHash = hash,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static string ComputeHash(string raw)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
