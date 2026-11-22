using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Sync;

public class SyncOperationItem
{
    [NotEmptyGuid]
    public Guid OperationId { get; set; }
    [Required]
    [RegularExpression("^(review\\.submit|card\\.create)$", ErrorMessage = "Unsupported sync operation type.")]
    public string Type { get; set; } = string.Empty;
    [NotEmptyGuid]
    public Guid EntityId { get; set; }
    [Range(0, int.MaxValue)]
    public int BaseVersion { get; set; } = 1;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public JsonElement Payload { get; set; }
}
