namespace LanguageLearning.Application.DTOs.Sync;

public class SyncOperationResult
{
    public Guid OperationId { get; set; }
    public string Status { get; set; } = "Accepted";
    public string Message { get; set; } = string.Empty;
}
