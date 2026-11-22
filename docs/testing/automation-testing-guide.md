# Automation Testing Guide

This document describes the automated test architecture for both Backend (Integration Testing) and Frontend (End-to-End Testing).

---

## 1. Backend Automation Testing (Integration Tests)

- **Project Path**: `backend/tests/LanguageLearning.IntegrationTests`
- **Framework**: .NET 8, xUnit, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), SQLite In-Memory.
- **Coverage**:
  - `AuthApiTests`: User registration, JWT login, authentication status, unauthorized rejection, profile update, and daily goal persistence.
  - `CoursesApiTests`: Language listing, course catalog querying with domain/level filters, and hierarchical course/unit/lesson detail resolution.
  - `ReviewsApiTests`: Spaced repetition (SM-2) review queue, review rating submission (`Again`, `Hard`, `Good`, `Easy`), and schedule recalculation.
  - `AdminApiTests`: Role-based access control (RBAC), preventing non-admin access (HTTP 403), user management, and language lifecycle (Create, Update, Delete).
  - `DecksAndCardsApiTests`: Full CRUD operations on decks and cards.

### Execution Command
```bash
# Run only integration tests
dotnet test backend/tests/LanguageLearning.IntegrationTests

# Run all backend tests (Unit + Integration: 56 tests total)
dotnet test backend/LanguageLearning.sln
```

---

## 2. Frontend Automation Testing (Playwright E2E)

- **Config Path**: `frontend/playwright.config.ts`
- **Test Specs Path**: `frontend/e2e/`
- **Framework**: `@playwright/test`
- **Coverage**:
  - `home.spec.ts`: Page loading, header/brand verification, and sidebar navigation across views.
  - `srs-review.spec.ts`: Opening SRS review, flashcard flipping with spacebar/click, and rating selection.
  - `auth.spec.ts`: Study target modal preferences and authentication dialog flows.

### Execution Commands
```bash
# Run headless E2E tests
npm run test:e2e

# Run with interactive UI dashboard
npm run test:e2e:ui

# Run in headed browser mode
npm run test:e2e:headed
```
