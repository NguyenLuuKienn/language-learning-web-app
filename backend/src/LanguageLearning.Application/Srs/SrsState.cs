namespace LanguageLearning.Application.Srs;
public sealed record SrsState(
    int Repetitions,
    double IntervalDays,
    double EaseFactor
);