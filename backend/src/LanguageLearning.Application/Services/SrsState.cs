using System.Text.Json;

namespace LanguageLearning.Application.Services;

public class SrsState
{
    public double IntervalDays { get; set; } = 0;
    public double EaseFactor { get; set; } = 2.5;
    public int Repetitions { get; set; } = 0;
    public int Lapses { get; set; } = 0;
    public DateTime DueAt { get; set; }

    public static SrsState FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return new SrsState();
        }

        try
        {
            return JsonSerializer.Deserialize<SrsState>(json) ?? new SrsState();
        }
        catch
        {
            return new SrsState();
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}
