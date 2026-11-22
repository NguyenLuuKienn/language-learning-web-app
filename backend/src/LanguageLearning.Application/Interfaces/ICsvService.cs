using LanguageLearning.Application.DTOs.Csv;

namespace LanguageLearning.Application.Interfaces;

public interface ICsvService
{
    Task<CsvPreviewResponse> PreviewAsync(Stream stream, CancellationToken cancellationToken = default);
    Task<CsvImportResultResponse> ImportAsync(Guid deckId, CommitCsvImportRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportDeckAsync(Guid deckId, Guid? userId = null, CancellationToken cancellationToken = default);
}
