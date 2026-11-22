using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Sync;

public class SyncPushRequest
{
    [NotEmptyGuid]
    public Guid DeviceId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(500)]
    public List<SyncOperationItem> Operations { get; set; } = new();
}
