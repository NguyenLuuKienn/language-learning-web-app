
using LanguageLearning.Application.Srs;
using LanguageLearning.Domain.Enums;
using Xunit;

namespace LanguageLearning.Tests;

public class SrsV1SchedulerTests
{
    private readonly SrsV1Scheduler _scheduler = new();

    [Fact]
    public void AlgorithmVersion_ShouldBeSrsV1()
    {
        Assert.Equal("srs-v1", _scheduler.AlgorithmVersion);
    }

    [Theory]
    [InlineData(SrsRating.Again, 0, 10.0 / 1440.0, 2.3)]
    [InlineData(SrsRating.Hard, 0, 1.0, 2.35)]
    [InlineData(SrsRating.Good, 1, 1.0, 2.5)]
    [InlineData(SrsRating.Easy, 1, 4.0, 2.65)]
    public void Schedule_NewCard_ShouldCalculateCorrectly(
        SrsRating rating,
        int expectedRepetitions,
        double expectedInterval,
        double expectedEase)
    {
        var initialState = new SrsState(
            Repetitions: 0,
            IntervalDays: 0,
            EaseFactor: 2.5
        );

        var reviewedAt = new DateTime(
            2026, 10, 25, 10, 0, 0,
            DateTimeKind.Utc
        );

        var (updatedState, dueAt) = _scheduler.Schedule(
            initialState,
            rating,
            reviewedAt
        );

        Assert.Equal(
            expectedRepetitions,
            updatedState.Repetitions
        );

        Assert.Equal(
            expectedInterval,
            updatedState.IntervalDays,
            8
        );

        Assert.Equal(
            expectedEase,
            updatedState.EaseFactor,
            8
        );

        Assert.Equal(
            reviewedAt.AddDays(expectedInterval),
            dueAt
        );
    }

[Theory]
[InlineData(SrsRating.Again, 0, 10.0 / 1440.0, 2.3)]
[InlineData(SrsRating.Hard, 2, 7.2, 2.35)]
[InlineData(SrsRating.Good, 3, 15.0, 2.5)]
[InlineData(SrsRating.Easy, 3, 19.5, 2.65)]
public void Schedule_ReviewedCard_ShouldCalculateCorrectly(
    SrsRating rating,
    int expectedRepetitions,
    double expectedInterval,
    double expectedEase)
{
    var currentState = new SrsState(
        Repetitions: 2,
        IntervalDays: 6,
        EaseFactor: 2.5
    );

    var reviewedAt = new DateTime(
        2026, 10, 25, 10, 0, 0,
        DateTimeKind.Utc
    );

    var (updatedState, dueAt) = _scheduler.Schedule(
        currentState,
        rating,
        reviewedAt
    );

    Assert.Equal(
        expectedRepetitions,
        updatedState.Repetitions
    );

    Assert.Equal(
        expectedInterval,
        updatedState.IntervalDays,
        8
    );

    Assert.Equal(
        expectedEase,
        updatedState.EaseFactor,
        8
    );

    Assert.Equal(
        reviewedAt.AddDays(updatedState.IntervalDays),
        dueAt
    );
}

[Theory]
[InlineData(SrsRating.Good, 1, 1.0, 2.5, 2, 6.0, 2.5)]
[InlineData(SrsRating.Hard, 2, 0.2, 2.5, 2, 1.0, 2.35)]
[InlineData(SrsRating.Easy, 1, 0.5, 2.5, 2, 4.0, 2.65)]
[InlineData(SrsRating.Again, 2, 6.0, 1.3, 0, 10.0 / 1440.0, 1.3)]
[InlineData(SrsRating.Hard, 2, 6.0, 1.3, 2, 7.2, 1.3)]
public void Schedule_EdgeCases_ShouldCalculateCorrectly(
    SrsRating rating,
    int repetitions,
    double intervalDays,
    double easeFactor,
    int expectedRepetitions,
    double expectedInterval,
    double expectedEase)
{
    var currentState = new SrsState(
        repetitions,
        intervalDays,
        easeFactor
    );

    var reviewedAt = new DateTime(
        2026, 10, 25, 10, 0, 0,
        DateTimeKind.Utc
    );

    var (updatedState, dueAt) = _scheduler.Schedule(
        currentState,
        rating,
        reviewedAt
    );

    Assert.Equal(
        expectedRepetitions,
        updatedState.Repetitions
    );

    Assert.Equal(
        expectedInterval,
        updatedState.IntervalDays,
        8
    );

    Assert.Equal(
        expectedEase,
        updatedState.EaseFactor,
        8
    );

    Assert.Equal(
    reviewedAt.AddDays(updatedState.IntervalDays),
    dueAt
    );
}

[Theory]
[InlineData(-1, 0.0, 2.5)]
[InlineData(0, -1.0, 2.5)]
[InlineData(0, double.NaN, 2.5)]
[InlineData(0, double.PositiveInfinity, 2.5)]
[InlineData(0, 1.0, double.NaN)]
[InlineData(0, 1.0, double.PositiveInfinity)]
[InlineData(0, 1.0, 1.2)]
public void Schedule_InvalidState_ShouldThrow(
    int repetitions,
    double intervalDays,
    double easeFactor)
{
    var state = new SrsState(
        repetitions,
        intervalDays,
        easeFactor
    );

    var reviewedAt = new DateTime(
        2026, 10, 25, 10, 0, 0,
        DateTimeKind.Utc
    );

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        _scheduler.Schedule(
            state,
            SrsRating.Good,
            reviewedAt
        )
    );
}

[Fact]
public void Schedule_NullState_ShouldThrow()
{
    var reviewedAt = DateTime.UtcNow;

    Assert.Throws<ArgumentNullException>(() =>
        _scheduler.Schedule(
            null!,
            SrsRating.Good,
            reviewedAt
        )
    );
}

[Theory]
[InlineData(DateTimeKind.Local)]
[InlineData(DateTimeKind.Unspecified)]
public void Schedule_NonUtcTime_ShouldThrow(
    DateTimeKind kind)
{
    var state = new SrsState(0, 0, 2.5);

    var reviewedAt = new DateTime(
        2026, 10, 25, 10, 0, 0,
        kind
    );

    Assert.Throws<ArgumentException>(() =>
        _scheduler.Schedule(
            state,
            SrsRating.Good,
            reviewedAt
        )
    );
}

[Fact]
public void Schedule_InvalidRating_ShouldThrow()
{
    var state = new SrsState(0, 0, 2.5);

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        _scheduler.Schedule(
            state,
            (SrsRating)99,
            DateTime.UtcNow
        )
    );
}

}
