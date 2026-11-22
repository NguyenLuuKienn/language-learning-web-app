namespace LanguageLearning.Application.Validation;

public static class CourseClassification
{
    private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        "general", "it", "business", "travel", "academic"
    };

    public static void Validate(string languageCode, string level, string domain)
    {
        if (!Domains.Contains(domain))
            throw new ArgumentException("Course domain is not supported.");

        var code = languageCode.Trim().ToLowerInvariant();
        var allowedLevels = code switch
        {
            "ja" or "jp" => new[] { "N5", "N4", "N3", "N2", "N1" },
            "zh" or "cn" => Enumerable.Range(1, 6).Select(number => $"HSK {number}"),
            "ko" or "kr" => Enumerable.Range(1, 6).Select(number => $"TOPIK {number}"),
            _ => new[] { "A1", "A2", "B1", "B2", "C1", "C2" }
        };
        if (!allowedLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Course level does not match the language framework.");
    }
}
