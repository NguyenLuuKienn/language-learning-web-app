using System.Text;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Csv;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;

namespace LanguageLearning.Application.Services;

public class CsvService : ICsvService
{
    private readonly ICardRepository _cardRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly IStudyAccessService _access;
    private readonly ICurrentUserService _currentUser;

    public CsvService(ICardRepository cardRepository, IDeckRepository deckRepository, IStudyAccessService access, ICurrentUserService currentUser)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<CsvPreviewResponse> PreviewAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                lines.Add(line);
            }
        }

        if (!lines.Any())
        {
            throw new ArgumentException(Messages.Csv.EmptyFile);
        }

        var headerTokens = ParseCsvLine(lines[0]);
        int frontIdx = -1;
        int backIdx = -1;

        for (int i = 0; i < headerTokens.Count; i++)
        {
            var h = headerTokens[i].Trim().ToLowerInvariant();
            if (h == "front" || h == "term" || h == "word") frontIdx = i;
            if (h == "back" || h == "meaning" || h == "definition") backIdx = i;
        }

        if (frontIdx == -1 || backIdx == -1)
        {
            if (headerTokens.Count >= 2)
            {
                frontIdx = 0;
                backIdx = 1;
            }
            else
            {
                throw new ArgumentException(Messages.Csv.HeaderMissing);
            }
        }

        var rows = new List<CsvRowPreview>();
        int validCount = 0;
        int invalidCount = 0;

        for (int r = 1; r < lines.Count; r++)
        {
            var rawLine = lines[r];
            if (string.IsNullOrWhiteSpace(rawLine)) continue;

            var tokens = ParseCsvLine(rawLine);
            string front = tokens.Count > frontIdx ? tokens[frontIdx].Trim() : string.Empty;
            string back = tokens.Count > backIdx ? tokens[backIdx].Trim() : string.Empty;

            bool isValid = true;
            string? errorMessage = null;

            if (string.IsNullOrWhiteSpace(front))
            {
                isValid = false;
                errorMessage = Messages.Csv.FrontEmpty;
            }
            else if (string.IsNullOrWhiteSpace(back))
            {
                isValid = false;
                errorMessage = Messages.Csv.BackEmpty;
            }

            if (isValid) validCount++;
            else invalidCount++;

            rows.Add(new CsvRowPreview
            {
                RowNumber = r + 1,
                Front = front,
                Back = back,
                IsValid = isValid,
                ErrorMessage = errorMessage
            });
        }

        return new CsvPreviewResponse
        {
            TotalRows = rows.Count,
            ValidRowsCount = validCount,
            InvalidRowsCount = invalidCount,
            Rows = rows
        };
    }

    public async Task<CsvImportResultResponse> ImportAsync(Guid deckId, CommitCsvImportRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
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

        var cards = request.Cards
            .Where(c => !string.IsNullOrWhiteSpace(c.Front) && !string.IsNullOrWhiteSpace(c.Back))
            .Select(c => new Card
            {
                Id = Guid.NewGuid(),
                DeckId = deckId,
                Front = c.Front.Trim(),
                Back = c.Back.Trim(),
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

        if (cards.Any())
        {
            await _cardRepository.AddBatchAsync(cards, cancellationToken);

            deck.Version += 1;
            deck.UpdatedAt = DateTime.UtcNow;
            _deckRepository.Update(deck);
            await _deckRepository.SaveChangesAsync(cancellationToken);
        }

        return new CsvImportResultResponse
        {
            ImportedCount = cards.Count,
            DeckId = deckId
        };
    }

    public async Task<byte[]> ExportDeckAsync(Guid deckId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var deck = await _deckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck == null)
        {
            throw new NotFoundException(Messages.Deck.DeckNotFound);
        }
        if (!userId.HasValue || (!_currentUser.IsAdmin() && !await _access.CanAccessDeckAsync(userId.Value, deckId, cancellationToken)))
            throw new NotFoundException(Messages.Deck.DeckNotFound);

        var cards = await _cardRepository.GetByDeckIdAsync(deckId, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Front,Back");

        foreach (var c in cards)
        {
            var frontEscaped = EscapeCsvField(c.Front);
            var backEscaped = EscapeCsvField(c.Back);
            sb.AppendLine($"{frontEscaped},{backEscaped}");
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetBytes(sb.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        var sanitized = field;
        if (sanitized.StartsWith('=') || sanitized.StartsWith('+') || sanitized.StartsWith('-') || sanitized.StartsWith('@'))
        {
            sanitized = "'" + sanitized;
        }

        sanitized = sanitized.Replace("\"", "\"\"");
        return $"\"{sanitized}\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }
}
