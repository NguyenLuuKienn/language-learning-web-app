# API Endpoints Reference Specification (v1.0.0-local)

This document provides a comprehensive reference of all implemented REST API endpoints across Modules M01 to M16 for the Language Learning platform.

All endpoints are prefixed with `/api/v1` and return standard JSON or RFC 7807 Problem Details on errors.

---

## M01: Authentication, Authorization & User Profile
- `POST /api/v1/auth/register`: Create a new user account and set HttpOnly session cookies; JSON contains the user profile and access expiry, never tokens.
- `POST /api/v1/auth/login`: Authenticate with email and password and set session cookies.
- `POST /api/v1/auth/refresh`: Rotate the refresh cookie and renew the access cookie.
- `POST /api/v1/auth/logout`: Revoke the refresh session and clear cookies.
- Cookie-authenticated writes require `X-CSRF-Token` matching the `ll_csrf` cookie.
- `GET /api/v1/placement/courses/{courseId}`: Get four-skill placement questions without answer keys.
- `POST /api/v1/placement/courses/{courseId}/submit`: Grade on the server and return a short-lived proof only when passed.
- `PUT /api/v1/me/study-target`: Requires the placement proof for initial selection or a language/level change.
- `GET /api/v1/me`: Retrieve current authenticated user profile, daily goal, streak, and timezone.
- `PUT /api/v1/me/profile`: Update display name and avatar URL.
- `PUT /api/v1/me/password`: Securely change password after verifying existing password.
- `PUT /api/v1/me/daily-goal`: Update daily target review cards count and IANA timezone.

## M02 & M03: Courses, Curriculum Units & Vocabulary
- `GET /api/v1/courses`: Retrieve active published courses for an authenticated learner.
- `GET /api/v1/courses/{id}`: Retrieve course detail with ordered curriculum units and lesson summaries.
- `GET /api/v1/lessons/{id}`: Retrieve lesson detail, status, and vocabulary items with phonetics and examples.
- `GET /api/v1/lessons/{lessonId}/vocabularies`: Retrieve all vocabulary entries belonging to a lesson.
- `POST /api/v1/admin/courses`: Admin endpoint to create new course curriculum.
- `PUT /api/v1/admin/courses/{id}`: Admin endpoint to edit course metadata and status.

## M04, M05 & M06: Decks, Flashcards & Spaced Repetition (SRS)
- `GET /api/v1/decks/{deckId}/cards`: Retrieve all cards belonging to a deck.
- `POST /api/v1/decks/{deckId}/cards`: Create a new flashcard with front, back, and optional tags.
- `POST /api/v1/decks/{deckId}/cards/batch`: Batch create up to 500 cards in a single atomic transaction.
- `GET /api/v1/cards/{id}`: Retrieve a single flashcard by GUID.
- `PUT /api/v1/cards/{id}`: Update card front, back, or tags.
- `DELETE /api/v1/cards/{id}`: Soft delete or remove card from deck.
- `GET /api/v1/reviews/due`: Retrieve cards due for review under SM-2 schedule (filtered by optional `deckId`, default limit 50).
- `POST /api/v1/reviews`: Submit review evaluation (`Again`, `Hard`, `Good`, `Easy`) and update SM-2 schedule.
- `GET /api/v1/reviews/history`: Retrieve recent review event log for current user.

## M07 & M08: Quiz Assessment & Learning Progress
- `GET /api/v1/lessons/{id}/quiz`: Retrieve randomized questions for a lesson (MultipleChoice, FillInBlank, Matching).
- `POST /api/v1/quiz/submit`: Submit answers for server-side evaluation, calculate percentage score, and record lesson progress.
- `GET /api/v1/progress/courses/{courseId}`: Retrieve progress statistics for an enrolled course.
- `GET /api/v1/progress/resume`: Get the next recommended lesson for the learner.

## M09 & M10: CSV Import/Export & Multimedia Storage
- `POST /api/v1/decks/{id}/imports/preview`: Upload CSV file (`multipart/form-data`) and receive row-by-row validation analysis without database writes.
- `POST /api/v1/decks/{id}/imports`: Commit validated card entries into deck.
- `GET /api/v1/decks/{id}/export`: Export deck cards as UTF-8 CSV with BOM and Excel DDE formula injection sanitization.
- `POST /api/v1/media/upload`: Upload image or audio file with MIME whitelist validation and size limitation (<= 10MB).

## M11 & M12: Statistics, Streaks & Activity Heatmap
- `GET /api/v1/statistics/overview`: Retrieve retention rate, total cards reviewed, mastered count, and daily goal status.
- `GET /api/v1/statistics/heatmap`: Retrieve 365-day activity records with intensity levels calculated according to the user's local timezone.

## M13: Community Decks & Deck Sharing
- `GET /api/v1/public/decks`: Discover public community decks with keyword and language search.
- `GET /api/v1/public/decks/{id}/preview`: Preview metadata and sample cards of a public deck.
- `POST /api/v1/public/decks/{id}/clone`: Deep-clone a public deck and all associated flashcards into the learner's personal collection.

## M14: Offline Synchronization Engine
- `POST /api/v1/sync/push`: Push queued local client operations with idempotency ledger keys (`clientOperationId`).
- `GET /api/v1/sync/pull`: Pull server changes newer than a client timestamp cursor.

## M15 & M16: Administration & Security Audit
- `GET /api/v1/admin/users`: List registered users with search and role filters (`Admin` only).
- `PATCH /api/v1/admin/users/{id}/status`: Toggle user account lock/active state (`Admin` only).
- `PATCH /api/v1/admin/users/{id}/role`: Elevate or demote user role between `Learner` and `Admin`.
- `GET /api/v1/admin/public-decks`: Review community decks for moderation.
- `PATCH /api/v1/admin/public-decks/{id}/visibility`: Override deck visibility for moderation.
- `GET /api/v1/admin/audit-logs`: Inspect administrative audit log history with IP addresses and timestamps.
