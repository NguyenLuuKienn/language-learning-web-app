namespace LanguageLearning.Application.DTOs.Statistics;

public class HeatmapDayResponse
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Level { get; set; }
}
