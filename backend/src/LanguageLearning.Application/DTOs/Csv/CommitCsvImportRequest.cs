using System.ComponentModel.DataAnnotations;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.DTOs.Csv;

public class CommitCsvImportRequest
{
    [Required]
    [MinLength(1)]
    public List<CsvCardRow> Cards { get; set; } = new();
}

public class CsvCardRow
{
    [Required]
    [NotBlank]
    public string Front { get; set; } = string.Empty;

    [Required]
    [NotBlank]
    public string Back { get; set; } = string.Empty;
}

public class CsvImportResultResponse
{
    public int ImportedCount { get; set; }
    public Guid DeckId { get; set; }
}
