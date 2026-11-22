using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Application.Services;


public class SrsScheduler : ISrsScheduler
{
    public string Version => "srs-v1";

    public SrsState ScheduleAfterLearning(bool correct, DateTime learnedAtUtc)
    {
        var minutes = correct ? 24 * 60 : 10;
        return new SrsState
        {
            Repetitions = 0,
            IntervalDays = minutes / 1440.0,
            DueAt = learnedAtUtc.AddMinutes(minutes)
        };
    }

    public SrsState CalculateNext(SrsState currentState, SrsRating rating, DateTime reviewedAtUtc)
    {
        var next = new SrsState
        {
            EaseFactor = currentState.EaseFactor,
            Repetitions = currentState.Repetitions,
            Lapses = currentState.Lapses,
            IntervalDays = currentState.IntervalDays
        };

        if (next.EaseFactor < 1.3)
        {
            next.EaseFactor = 2.5;
        }

        switch (rating)
        {
            case SrsRating.Again:
                next.Repetitions = 0;
                next.Lapses += 1;
                next.EaseFactor = Math.Max(1.3, next.EaseFactor - 0.2);
                next.IntervalDays = 10.0 / 1440.0;
                next.DueAt = reviewedAtUtc.AddMinutes(10);
                break;

            case SrsRating.Hard:
                next.Repetitions += 1;
                next.EaseFactor = Math.Max(1.3, next.EaseFactor - 0.15);
                next.IntervalDays = next.IntervalDays <= 0 ? 1 : Math.Round(next.IntervalDays * 1.2, 2);
                next.DueAt = reviewedAtUtc.AddDays(next.IntervalDays);
                break;

            case SrsRating.Good:
                next.Repetitions += 1;
                if (next.Repetitions == 1)
                {
                    next.IntervalDays = 1;
                }
                else if (next.Repetitions == 2)
                {
                    next.IntervalDays = 6;
                }
                else
                {
                    next.IntervalDays = Math.Round(next.IntervalDays * next.EaseFactor, 2);
                }
                next.DueAt = reviewedAtUtc.AddDays(next.IntervalDays);
                break;

            case SrsRating.Easy:
                next.Repetitions += 1;
                next.EaseFactor += 0.15;
                if (next.Repetitions == 1)
                {
                    next.IntervalDays = 4;
                }
                else
                {
                    next.IntervalDays = Math.Round(next.IntervalDays * next.EaseFactor * 1.3, 2);
                }
                next.DueAt = reviewedAtUtc.AddDays(next.IntervalDays);
                break;
        }

        return next;
    }
}
