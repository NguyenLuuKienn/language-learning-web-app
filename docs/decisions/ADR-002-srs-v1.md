
# ADR-002: SRS Algorithm v1

## Status

Proposed

## Context

The application uses Spaced Repetition (SRS) to calculate
the next review time for flashcards.

The scheduler supports four ratings:
Again, Hard, Good, and Easy.

The algorithm is isolated behind ISrsScheduler so that
future versions can be implemented independently.

## Algorithm Version

Version: `srs-v1`

Implementation: `SrsV1Scheduler`

Interface: `ISrsScheduler`

## State

Each card review uses the following state:

- Repetitions: consecutive successful review count.
- IntervalDays: current review interval in days.
- EaseFactor: multiplier for future intervals.

Initial state:

- Repetitions = 0
- IntervalDays = 0
- EaseFactor = 2.5

## Scheduling Rules

### Again

- Reset Repetitions to 0.
- Set IntervalDays to 10 / 1440 (10 minutes).
- Decrease EaseFactor by 0.2, minimum 1.3.

### Hard

- Keep Repetitions unchanged.
- If Repetitions = 0, IntervalDays = 1.
- Otherwise, IntervalDays = max(1, old interval * 1.2).
- Decrease EaseFactor by 0.15, minimum 1.3.

### Good

- Increase Repetitions by 1.
- If previous Repetitions = 0, IntervalDays = 1.
- If previous Repetitions = 1, IntervalDays = 6.
- Otherwise, IntervalDays = old interval * EaseFactor.
- Keep EaseFactor unchanged.

### Easy

- Increase Repetitions by 1.
- If previous Repetitions = 0, IntervalDays = 4.
- Otherwise, IntervalDays = max(4, old interval * EaseFactor * 1.3).
- Increase EaseFactor by 0.15.

## Due Date

DueAt = ReviewedAtUtc + IntervalDays

The review timestamp must use UTC.

## Validation

The scheduler rejects:

- Null state.
- Negative Repetitions.
- Negative or non-finite IntervalDays.
- Non-finite EaseFactor or values below 1.3.
- Non-UTC review timestamps.
- Invalid rating values.

## Versioning

The scheduler exposes AlgorithmVersion = "srs-v1".

CardSchedule and ReviewEvent already contain an
AlgorithmVersion field.

Future algorithm changes should introduce a new
version rather than silently changing the behavior
of srs-v1.

## Testing

Unit tests cover:

- All four rating values.
- New cards and previously reviewed cards.
- Boundary conditions.
- Invalid state and rating values.
- Non-UTC timestamps.
- Algorithm version.

The SrsV1Scheduler implementation achieves 100%
line and branch coverage in the current Coverlet report.

## Scope

This ADR describes scheduling calculations only.

Persisting card schedules, querying due cards,
and recording review events belong to Issue #25.

Review submission idempotency and concurrency
handling belong to Issue #27.
