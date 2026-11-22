namespace LanguageLearning.Application.DTOs.Sync;

public class SyncPushResponse
{
    public int ProcessedCount { get; set; }
    public List<SyncOperationResult> Results { get; set; } = new();
}
