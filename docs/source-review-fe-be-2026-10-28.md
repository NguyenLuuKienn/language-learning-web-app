# FE/BE source review — 2026-10-28

## Scope and method

Reviewed the current working tree against `docs/standards/coding-and-ai-guidelines.md`, `docs/api/api-conventions.md`, `docs/decisions/ADR-001-guid-identity-and-architecture.md`, the learning journey redesign, and the existing production readiness review. Traced the main learner journey, skill progress, SRS persistence, admin material editing, API initialization, and frontend build/lint paths. This is a source review with automated build and test verification, not a claim that every browser/API/MySQL path was exercised.

## Findings (highest priority first)

### P2 — Schema is upgraded at startup, but EF migration history is not the active path

`backend/src/LanguageLearning.Api/Program.cs:283-300` calls `EnsureCreated()` and then 17 `LegacySchemaUpgrade` methods on every startup, while EF migration files also exist. The application **does upgrade the existing local MySQL schema** through those legacy methods; the previous wording wrongly implied that no migration was running. On 2026-10-29, the local database contained `learning_sessions` but did not contain `__EFMigrationsHistory`, confirming that EF migration history is not the path used by this runtime. The risk is having two schema definitions to maintain: a new EF migration alone will not update the local database unless the startup upgrade is also changed. This is a release-maintenance risk, not evidence that the current MySQL schema is broken.

**Recommendation:** converge on versioned migrations, rehearse them against a copy of the local MySQL database, verify indexes/FKs and row counts, and stop performing ad hoc DDL in the API startup path. Keep startup failure explicit until the migration is complete.

### P1 — Concurrent reviews of one card can lose an SRS transition

`backend/src/LanguageLearning.Application/Services/SrsService.cs:152-198` reads the schedule and calculates the next state before entering the repository transaction. `backend/src/LanguageLearning.Infrastructure/Repositories/ReviewRepository.cs:203-230` then reloads the row but writes the already calculated state without checking whether another review changed it. Two distinct `ClientEventId` requests can each commit a review event while the later write replaces the first scheduling transition. The idempotency check protects duplicate event IDs only. This is a source-level race; it has not been reproduced with a concurrent MySQL test.

**Recommendation:** lock or version the schedule row inside the transaction, calculate the transition from the latest locked state, and add a two-request concurrency test using distinct event IDs.

### P1 — A database password is stored in tracked development configuration

`backend/src/LanguageLearning.Api/appsettings.Development.json` is tracked by Git and contains a non-placeholder password in `ConnectionStrings:DefaultConnection` (also present in `HEAD`). The value is intentionally omitted here. Even if this is only a local account, the repository distributes the credential and conflicts with the secret-handling rule in `docs/standards/coding-and-ai-guidelines.md`.

**Recommendation:** move the value to user secrets or an ignored local environment file, replace the tracked file with a safe example, and rotate that MySQL password if it was shared or reused. Removing it from a later commit does not erase Git history.

### P2 — Skill progress list is unbounded and does not follow the list API convention

`backend/src/LanguageLearning.Api/Controllers/SkillPracticeController.cs:13-15` exposes a list without `page`/`pageSize`; `backend/src/LanguageLearning.Infrastructure/Repositories/SkillPracticeRepository.cs:76-79` returns all matching rows with `ToListAsync`. As learners accumulate activities, this response and its DB read grow indefinitely. `docs/api/api-conventions.md` specifies paginated list responses with a maximum page size of 50.

**Recommendation:** add page parameters and a total count/envelope, or provide a bounded course/lesson-scoped summary endpoint if the UI needs only current content. Update FE consumption together with the API.

### P2 — FE lint passes with warnings in journey and practice state handling

`npm.cmd run lint` exits 0 but reports 77 warnings, including missing `useEffect` dependencies in `frontend/src/features/journey/LessonPlayerView.tsx:210-223` and practice views, plus state updates inside effects. A successful lint exit therefore does not establish that hooks are free from stale state or redundant renders. These are static warnings, not individually proven learner-facing failures.

**Recommendation:** resolve the hooks warnings on the lesson player and practice views first, then make new lint warnings fail CI. Review each effect's data ownership instead of adding dependencies mechanically.

## Verified safeguards

- Admin lesson editing checks active learner state and rejects changes to activity arrays and step completion contracts in `CourseMaterialService.cs:370-388`; this path should be kept.
- SRS submissions use `ClientEventId` to detect retries; the race above concerns distinct concurrent IDs.
- FE production build passed (`npm.cmd run build`).
- BE Release build passed (`dotnet build backend/LanguageLearning.sln --configuration Release --no-restore`) with 3 `NU1900` warnings because NuGet vulnerability data could not be fetched in this environment.
- BE tests passed: 53 unit and 66 integration, no failures (`dotnet test ... --configuration Release --no-build`).

## Remaining verification

The review did not run a browser-to-API-to-local-MySQL smoke test, a concurrent SRS write test, or a production-like schema migration/rollback rehearsal. Those are needed before treating the source findings as closed. The working tree already contains extensive changes; this report does not modify application code or reset those changes.
