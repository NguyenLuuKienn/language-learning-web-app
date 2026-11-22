using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Enums;
using Xunit;

namespace LanguageLearning.Tests;

public class SrsSchedulerTests
{
    private readonly SrsScheduler _scheduler = new();

    [Fact]
    public void LearningCreatesFirstDueDateWithoutCountingAReview()
    {
        var now = DateTime.UtcNow;
        var passed = _scheduler.ScheduleAfterLearning(true, now);
        var missed = _scheduler.ScheduleAfterLearning(false, now);

        Assert.Equal(0, passed.Repetitions);
        Assert.Equal(0, missed.Repetitions);
        Assert.Equal(0, passed.Lapses);
        Assert.Equal(0, missed.Lapses);
        Assert.Equal(now.AddDays(1), passed.DueAt);
        Assert.Equal(now.AddMinutes(10), missed.DueAt);
        Assert.Equal(1, _scheduler.CalculateNext(passed, SrsRating.Good, passed.DueAt).Repetitions);
    }

    [Fact]
    public void CalculateNext_WithAgain_ShouldResetRepetitionsAndScheduleIn10Minutes()
    {
        var now = DateTime.UtcNow;
        var initial = new SrsState
        {
            EaseFactor = 2.5,
            Repetitions = 3,
            IntervalDays = 10,
            Lapses = 0
        };

        var next = _scheduler.CalculateNext(initial, SrsRating.Again, now);

        Assert.Equal(0, next.Repetitions);
        Assert.Equal(1, next.Lapses);
        Assert.Equal(2.3, Math.Round(next.EaseFactor, 2));
        Assert.True(next.DueAt > now);
        Assert.True(next.DueAt <= now.AddMinutes(11));
    }

    [Fact]
    public void CalculateNext_WithGoodFirstReview_ShouldScheduleIn1Day()
    {
        var now = DateTime.UtcNow;
        var initial = new SrsState { Repetitions = 0, EaseFactor = 2.5 };

        var next = _scheduler.CalculateNext(initial, SrsRating.Good, now);

        Assert.Equal(1, next.Repetitions);
        Assert.Equal(1.0, next.IntervalDays);
        Assert.Equal(now.AddDays(1).Date, next.DueAt.Date);
    }

    [Fact]
    public void CalculateNext_WithGoodSecondReview_ShouldScheduleIn6Days()
    {
        var now = DateTime.UtcNow;
        var initial = new SrsState { Repetitions = 1, IntervalDays = 1.0, EaseFactor = 2.5 };

        var next = _scheduler.CalculateNext(initial, SrsRating.Good, now);

        Assert.Equal(2, next.Repetitions);
        Assert.Equal(6.0, next.IntervalDays);
        Assert.Equal(now.AddDays(6).Date, next.DueAt.Date);
    }

    [Fact]
    public void CalculateNext_WithEasyFirstReview_ShouldScheduleIn4Days()
    {
        var now = DateTime.UtcNow;
        var initial = new SrsState { Repetitions = 0, EaseFactor = 2.5 };

        var next = _scheduler.CalculateNext(initial, SrsRating.Easy, now);

        Assert.Equal(1, next.Repetitions);
        Assert.Equal(4.0, next.IntervalDays);
        Assert.Equal(2.65, Math.Round(next.EaseFactor, 2));
    }

    [Fact]
    public void CalculateNext_GoodProgressionAndLapse_ShouldPreserveReviewHistory()
    {
        var now = DateTime.UtcNow;
        var first = _scheduler.CalculateNext(new SrsState(), SrsRating.Good, now);
        var second = _scheduler.CalculateNext(first, SrsRating.Good, first.DueAt);
        var third = _scheduler.CalculateNext(second, SrsRating.Good, second.DueAt);
        var lapse = _scheduler.CalculateNext(third, SrsRating.Again, third.DueAt);

        Assert.Equal(15, third.IntervalDays);
        Assert.Equal(3, third.Repetitions);
        Assert.Equal(0, lapse.Repetitions);
        Assert.Equal(1, lapse.Lapses);
        Assert.Equal(third.DueAt.AddMinutes(10), lapse.DueAt);
    }

    [Fact]
    public void CalculateNext_Hard_ShouldExtendIntervalAndReduceEase()
    {
        var now = DateTime.UtcNow;
        var next = _scheduler.CalculateNext(new SrsState { IntervalDays = 5, EaseFactor = 2.5, Repetitions = 2 }, SrsRating.Hard, now);

        Assert.Equal(6, next.IntervalDays);
        Assert.Equal(2.35, Math.Round(next.EaseFactor, 2));
        Assert.Equal(now.AddDays(6), next.DueAt);
    }
}
