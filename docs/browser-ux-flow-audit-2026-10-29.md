# Browser UI/UX and learner-flow audit — 2026-10-29

## Implementation update

The findings below describe the pre-change browser state. The first follow-up pass now removes the fabricated Home target, routes the Home course shortcut to the roadmap, uses one prerequisite state for target-dependent screens, distinguishes curriculum load errors from empty content, and translates the placement exam chrome and landing course examples in EN/VI. FE build passes. Browser recheck confirmed that Home without a saved target shows the onboarding call to action, Grammar shows the same prerequisite instead of an invented A1 empty state, and the first placement question and navigation render in EN after switching language. Full lesson/skill/SRS interactions still require a disposable learner with a completed placement result.

The final check for this pass also translated the remaining visible vocabulary catalog prompts, confirmed the landing course calls to action point to the roadmap, rebuilt FE and BE successfully, and passed 53 backend unit tests plus 66 backend integration tests. The sandboxed `dotnet test` run could not connect to `testhost`; an unsandboxed rerun passed. The remaining work is a complete lesson-to-SRS browser journey on disposable data, wider admin i18n, and replacement or explicit labeling of unverified landing statistics.

## Environment and limits

- Opened Vite at `http://127.0.0.1:5173` and the API at `http://localhost:5039`, connected to the user's local MySQL. Logged in using the repository's seeded learner test account. Used browser navigation and read-only UI actions; no lesson answers, submissions, imports, or deletes were made.
- The installed Playwright package could not launch its bundled Chromium because the browser executable is missing. The 10 attempted E2E tests failed at browser launch, **before application assertions**. Manual browser inspection below used the available in-app browser instead.
- The learner account has no accepted placement proof/current study target at the end of restoration. This blocks exercise content. Thus a screen opening successfully is not a pass for its learning interaction, progress persistence, audio, or answer feedback.

## Confirmed browser findings

### P1 — Home presents a course target that is not actually selected

Immediately after login, Home showed `English / A1 • GENERAL` and a course shortcut. The shortcut led to `/courses`, where the UI required a study target and placement test; `/roadmap` then displayed only independent-tool links, without the course → unit → lesson journey. `frontend/src/features/home/HomeView.tsx:110-114` fills a missing target with the literal defaults `A1`, `general`, and `English`. This makes an unconfigured account look configured and sends the learner into a dead end. The target-restoration flow in `frontend/src/App.tsx:241-273` can open the target modal after the initial Home render.

**Change:** show a clear onboarding state while target restoration is pending; after restoration, render either the actual target and next lesson or a single placement/start action. Do not display fallback language/level/domain as if saved.

### P1 — Placement test ignores the selected UI language

With UI language set to EN, selecting `Start Learning Journey` opened the placement exam in Vietnamese: `Đánh Giá Năng Lực Xếp Lớp`, `Nghe Audio Bản Xứ`, `Câu Trước`, `Nộp Bài Thi`. These strings are literal in `frontend/src/features/quiz/ExamRunnerModal.tsx:287`, `:495`, and `:624` despite the component importing `useTranslation`. The exam is a required gate, so this affects the primary learner flow, not just an optional screen.

**Change:** move all exam chrome, status, validation and result strings into the same translation pattern as the app shell; keep question content in its authored language.

### P2 — Landing page mixes languages and shows fixed demo/course claims

In VI mode, the SRS demo still says `SM-2 Interval Calculator • Stage: Review Due`, and the course cards keep English titles/descriptions and `5 Lessons`. The displayed retention percentages and catalog cards are fixed UI content (`frontend/src/features/landing/LandingPage.tsx:324`, `:378`, `:676`, `:725-735`) rather than account/catalog data. This makes the first screen look disconnected from the actual courses and stats. The numeric claims should be sourced or clearly presented as illustrative demo values.

**Change:** translate the demo and card copy; distinguish example content from live catalog data, or load published courses. Review unverified retention claims before release.

### P2 — Empty states disagree about why content is unavailable

Without a selected target, Listening, Speaking, Reading, Writing, Library, AI Conversation, Vocabulary and Review explain that placement is required. Grammar instead displays `A1`, `0 grammar topics`, and `No grammar content has been published` (`frontend/src/features/practice/GrammarView.tsx:144` uses `target?.level || 'A1'`). Quiz says `No published courses are available yet`; IELTS/TOEIC and Interview say no matching practice at “this level/goal”. These messages lead a learner to think content is missing when the real prerequisite is incomplete.

**Change:** use one target/placement prerequisite component for gated features. Show “no content” only after a valid target and successful catalog response. Do not invent A1 as the current level.

### P2 — Community deck store shows a prerequisite banner alongside its own content

`/community` displayed the placement-required notice and `Public Deck Community Store` on the same page. The store was empty for this account, but the mixed presentation leaves it unclear whether community decks are independently usable or locked by the course target.

**Change:** decide and express the access rule once; hide or disable the store when locked, or remove the course prerequisite banner if community decks are intentionally independent.

## Feature-by-feature browser coverage

| Surface | Observed state | Interaction coverage |
| --- | --- | --- |
| Landing / login | EN↔VI toggle, login modal and learner login worked | Navigation and language switch checked |
| Home | Rendered a default-looking English A1 General target before onboarding prompt | Course shortcut checked; found P1 flow mismatch |
| Target / placement | Target selector opened; selecting start opened a 20-question exam | First question and close/return checked; exam not submitted |
| Roadmap / vocabulary | Roadmap had tool links only; vocabulary requested target + placement | Lesson entry, resume and vocabulary loop blocked |
| Listening / speaking / reading / writing | Routes rendered prerequisite message | Exercises, audio and progress blocked |
| Grammar | Rendered A1/zero-content empty state without a target | Filter/answer interaction blocked |
| Character lab / library / AI chat | Routes rendered prerequisite message | Learning interaction blocked |
| SRS review / My decks | Routes rendered prerequisite message | Scheduling and deck CRUD blocked |
| IELTS / TOEIC | Separate tabs worked; each showed a neutral empty state | Exam attempt blocked by missing target/content |
| Interview | Neutral empty state rendered | Attempt blocked by missing target/content |
| Quiz | Showed placement action and “no published courses” | Quiz attempt blocked |
| Community decks | Prerequisite notice and empty public store both rendered | Clone action unavailable |
| Notebook | Opened on Review; new-note and empty state translated EN↔VI | Save/delete not exercised to preserve existing data |
| Statistics / Sync | Both rendered zero/empty dashboards | Cross-feature progress and offline replay not exercised |

## Next verification pass

Use a disposable MySQL clone with a learner who has a completed placement result and a populated course. Run one complete course → unit → lesson → skills → quiz → SRS cycle, check refresh/resume and the same paths in VI/EN and narrow viewport. Add assertions for the two confirmed i18n/empty-state defects and the Home target state. The current E2E suite's guided lesson test is skipped unless `PLAYWRIGHT_GUIDED_E2E=1`; it must run only on a disposable database.
