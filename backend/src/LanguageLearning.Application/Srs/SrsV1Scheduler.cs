
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Srs;

public sealed class SrsV1Scheduler : ISrsScheduler
{
    public string AlgorithmVersion => "srs-v1";

    public (SrsState UpdatedState, DateTime DueAt) Schedule(
        SrsState currentState,
        SrsRating rating,
        DateTime reviewedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        if (reviewedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Review time must be UTC.",
                nameof(reviewedAtUtc));
        }

        if (currentState.Repetitions < 0 ||
            !double.IsFinite(currentState.IntervalDays) ||
            currentState.IntervalDays < 0 ||
            !double.IsFinite(currentState.EaseFactor) ||
            currentState.EaseFactor < 1.3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentState));
        }

        double ease = currentState.EaseFactor;

        SrsState updatedState = rating switch
        {
            SrsRating.Again => new SrsState(
                0,
                10.0 / 1440.0,
                Math.Max(1.3, ease - 0.2)
            ),

            SrsRating.Hard => new SrsState(
                currentState.Repetitions,
                currentState.Repetitions == 0
                    ? 1
                    : Math.Max(1, currentState.IntervalDays * 1.2),
                Math.Max(1.3, ease - 0.15)
            ),

            SrsRating.Good => new SrsState(
                currentState.Repetitions + 1,
                currentState.Repetitions == 0
                    ? 1
                    : currentState.Repetitions == 1
                        ? 6
                        : currentState.IntervalDays * ease,
                ease
            ),

            SrsRating.Easy => new SrsState(
                currentState.Repetitions + 1,
                currentState.Repetitions == 0
                    ? 4
                    : Math.Max(
                        4,
                        currentState.IntervalDays * ease * 1.3),
                ease + 0.15
            ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(rating))
        };

        DateTime dueAt = reviewedAtUtc.AddDays(
            updatedState.IntervalDays);

        return (updatedState, dueAt);
    }
}
