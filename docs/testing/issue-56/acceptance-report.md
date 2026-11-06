
# Issue #56 - Acceptance Test Report

## Scope

Acceptance testing for TC-01 to TC-20 based on
LANGUAGE_LEARNING_PROJECT_DOCUMENTATION.md.

## Environment

- Branch: test/56-acceptance-tests
- Backend: ASP.NET Core 8
- Database: MySQL test database
- Frontend: React / Vite
- Environment: Local testing

## Test Summary

- Total: 20
- Passed: 8
- Partial: 3
- Failed: 0
- Blocked: 9
- Not Run: 0

## Test Results

| Test Case | Status | Evidence | Notes |
|---|---|---|---|
| TC-01 | Pass | evidence/TC01-TC03.trx | Duplicate email rejected with HTTP 400 |
| TC-02 | Pass | evidence/TC01-TC03.trx | Learner denied HTTP 403; Admin allowed |
| TC-03 | Pass | evidence/TC03-API.trx | Unauthorized Deck update returns HTTP 404; no changes saved |
| TC-04 | Pass | PublishedContentRepositoryTests | Admin publishes draft course; published course becomes visible to learner queries |
| TC-05 | Blocked | - | Quiz grading and submission functionality is not yet available on develop; depends on #28 and #29 |
| TC-06 | Pass | evidence/TC06-SRS.txt | SRS v1: all four ratings verified for new and reviewed cards |
| TC-07 | Pass | evidence/TC07-ReviewIdempotency.txt | Duplicate review applied only once |
| TC-08 | Blocked | - | CSV preview/validation not available on develop; depends on #33 |
| TC-09 | Blocked | - | CSV export and formula injection protection not available; depends on #34 |
| TC-10 | Blocked | - | Media upload validation not available; depends on #35 |
| TC-11 | Blocked | - | Public deck cloning API not available on develop; depends on #42 |
| TC-12 | Partial | evidence/TC12-PrivateDeck.txt | Direct private deck access returns HTTP 404; public catalog check pending #41 |
| TC-13 | Blocked | - | Streak calculation across date boundaries and timezones not implemented on develop; depends on #39 |
| TC-14 | Pass | evidence/TC14-OfflineReview.png, evidence/TC14-SyncQueue.png | Reviewed 2 cards offline; 2 pending sync operations saved in IndexedDB |
| TC-15 | Pass | evidence/TC15-OfflineReload.png | PWA reloads offline; 2 Pending and 2 Synced operations remain in IndexedDB |
| TC-16 | Partial | evidence/TC16-BackendSync.txt | Backend concurrent sync is idempotent; frontend reconnect/retry verification pending PR #95 |
| TC-17 | Blocked | - | Deck version conflict handling not merged into develop; depends on PR #94 |
| TC-18 | Partial | evidence/TC18-Logout.txt | Logout and refresh token revocation passed; password change functionality not available for acceptance testing |

| TC-19 | Blocked | - | Offline media fallback not implemented on develop; depends on #36 |
| TC-20 | Blocked | - | Admin public deck moderation not available on develop; depends on #42 |


## Defects and Blockers

### Blocked Cases

| Test Case | Dependency |
|---|---|
| TC-05 | #28, #29 - Quiz |
| TC-08 | #33 - CSV preview |
| TC-09 | #34 - CSV export |
| TC-10 | #35 - Media upload |
| TC-11 | #42 - Deck cloning |
| TC-13 | #39 - Streak calculation |
| TC-17 | PR #94 - Deck version conflicts |
| TC-19 | #36 - Offline media fallback |
| TC-20 | #42 - Deck moderation |

### Partially Verified

| Test Case | Remaining Work |
|---|---|
| TC-12 | Verify public catalog visibility after #41 |
| TC-16 | Verify frontend reconnect/retry after PR #95 |
| TC-18 | Verify password change functionality |

## Final Acceptance

Not yet accepted.

All 20 test cases have been assessed, but only
8 are fully passed.

Remaining cases require implementation,
integration, or additional verification.
