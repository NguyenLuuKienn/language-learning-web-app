namespace LanguageLearning.Application.DTOs.Csv;

public class CsvPreviewResponse
{
    public int TotalRows { get; set; }
    public int ValidRowsCount { get; set; }
    public int InvalidRowsCount { get; set; }
    public List<CsvRowPreview> Rows { get; set; } = new();
}

public class CsvRowPreview
{
    public int RowNumber { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
