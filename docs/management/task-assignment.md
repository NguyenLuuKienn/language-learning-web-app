# BẢNG PHÂN CHIA NHIỆM VỤ DỰ ÁN (3 FULLSTACK DEVS + 1 LEADER/AI REVIEWER)

> **Repository:** [language-learning-web-app](https://github.com/NguyenLuuKienn/language-learning-web-app)  
> **Cập nhật ngày:** 14/10/2026  
> **Tất cả 62 Issues đã được gán trực tiếp trên GitHub Issues.**

---

## 👑 1. LEADER KIÊM MAINTAINER — GitHub: `@NguyenLuuKienn`
- **Vai trò:** Quản trị Monorepo, Reviewer tối cao, Kiến trúc sư hệ thống & Phụ trách Tích hợp AI.
- **Tổng số Issues:** **15 Issues** (`#01` - `#09`, `#55`, `#58` - `#62`).

| Issue | Tiêu đề | Loại | Trạng thái |
| :---: | :--- | :---: | :---: |
| **#01** | Khởi tạo Monorepo, README & LICENSE | Task/Docs | **Đã xong** |
| **#02** | Thiết lập GitHub Projects, labels, milestones, templates | Task | **Đã xong** |
| **#03** | Cấu hình frontend React/TS/Tailwind/Router & Notebook UI | Feature | **Đã xong** |
| **#04** | Khởi tạo ASP.NET Core solution & Clean Architecture DI layers | Feature | **Đã xong** |
| **#05** | Cấu hình MySQL, EF Core provider & Migrations | Feature | **Đã xong** |
| **#06** | Docker Compose + env.example + seed script | Task | **Đã xong** |
| **#07** | Thiết lập CI frontend/backend (GitHub Actions) | Task | **Đã xong** |
| **#08** | Thiết kế ERD và ADR GUID/UUID | Docs | **Đã xong** |
| **#09** | Thiết kế OpenAPI conventions, Problem Details | Docs | **Đã xong** |
| **#55** | Security and access-control tests (IDOR, SQLi, CORS, JWT) | Test | Chờ thực hiện |
| **#58** | Deploy staging, smoke test, health checks | Task | Chờ thực hiện |
| **#59** | Production deployment + backups/restore drill | Task | Chờ thực hiện |
| **#60** | Release notes, screenshots, demo script | Docs | Chờ thực hiện |
| **#61** | Contribution guide, code of conduct, security policy | Docs | Chờ thực hiện |
| **#62** | v1.0.0 release, final retrospective | Task | Chờ thực hiện |

* **Nhiệm vụ điều hành & AI:**
  * Review và Approve tất cả Pull Request từ các thành viên.
  * Nghiên cứu tích hợp các tính năng AI: AI sinh ngữ cảnh từ vựng, AI sinh bài tập trắc nghiệm, Speech-To-Text luyện phát âm.

---

## 📚 2. CORE DEVELOPER 1 — GitHub: `@MaitPhap`
- **Phân hệ phụ trách:** **Auth, Profile, Khóa học, Bài học, Từ vựng, Media & Admin (Fullstack)**.
- **Tổng số Issues:** **15 Issues** (`#10` - `#19`, `#35`, `#36`, `#51`, `#52`, `#57`).

| Issue | Tiêu đề | Phân tầng |
| :---: | :--- | :---: |
| **#10** | API đăng ký, đăng nhập, hash mật khẩu | Backend |
| **#11** | JWT / refresh / logout và authorization middleware | Backend |
| **#12** | Frontend auth screens + protected routes | Frontend |
| **#13** | User profile + timezone settings | Fullstack |
| **#14** | CRUD languages/courses/units/lessons admin API | Backend |
| **#15** | Publish/unpublish, ordering, validation logic | Backend |
| **#16** | Course catalog và course detail UI | Frontend |
| **#17** | Lesson viewer và navigation UI | Frontend |
| **#18** | Vocabulary CRUD + search/filter API | Backend |
| **#19** | Vocabulary UI và nội dung mẫu A1 | Frontend |
| **#35** | Media storage abstraction, upload validation | Backend |
| **#36** | Image/audio preview và playback UI | Frontend |
| **#51** | Admin user management và moderation API | Backend |
| **#52** | Admin course/content editor UI | Frontend |
| **#57** | Seed A1: 5 lessons, 50+ words, quiz content | Data Seed |

---

## 🧠 3. CORE DEVELOPER 2 — GitHub: `@nguyenhqbinh`
- **Phân hệ phụ trách:** **Bộ thẻ Flashcard, Thuật toán SRS & Ngoại tuyến PWA Sync (Fullstack)**.
- **Tổng số Issues:** **16 Issues** (`#20` - `#27`, `#44` - `#50`, `#56`).

| Issue | Tiêu đề | Phân tầng |
| :---: | :--- | :---: |
| **#20** | Deck CRUD, tags, visibility API | Backend |
| **#21** | My Decks UI, editor và search | Frontend |
| **#22** | Card CRUD API và validations | Backend |
| **#23** | Flashcard editor, flip animation, shortcuts | Frontend |
| **#24** | Đặc tả SRS algorithm, tests và versioning | Backend |
| **#25** | Card schedule, due query, review events | Backend |
| **#26** | Review session UI và rating buttons | Frontend |
| **#27** | Idempotent review submission và concurrency tests | Backend |
| **#44** | PWA manifest, service worker, app shell cache | FE / PWA |
| **#45** | IndexedDB schema và deck predownload | FE / Offline |
| **#46** | Offline review queue và pending UI | Frontend |
| **#47** | Sync push endpoint + idempotency ledger | Backend |
| **#48** | Sync pull cursor, entity versions, conflicts | Backend |
| **#49** | Client retry/backoff/reconciliation | Frontend |
| **#50** | Offline->online integration test, multi-device conflict | Integration |
| **#56** | CSV, SRS, streak, offline/sync acceptance test | Acceptance |

---

## 🎯 4. CONTRIBUTOR — GitHub: `[Chưa Assign / Mở cho cộng đồng]`
- **Phân hệ phụ trách:** **Luyện tập Quiz, Tiến độ học, Chuỗi Streak, Thống kê, CSV & Thư viện (Fullstack)**.
- **Tổng số Issues:** **16 Issues** (`#28` - `#34`, `#37` - `#43`, `#53`, `#54`).

| Issue | Tiêu đề | Phân tầng |
| :---: | :--- | :---: |
| **#28** | Question models/API multiple choice | Backend |
| **#29** | Fill-in-blank và matching question types | Backend |
| **#30** | Quiz UI, score, results và history | Frontend |
| **#31** | Lesson progress + enrollments API | Backend |
| **#32** | Resume learning + progress UI | Frontend |
| **#33** | CSV template và import preview/validation | Fullstack |
| **#34** | CSV import commit + export + security tests | Backend |
| **#37** | Review/day, retention, heatmap API | Backend |
| **#38** | Dashboard charts và filters | Frontend |
| **#39** | Daily goal/timezone/streak calculation | Backend |
| **#40** | Streak UI, calendar, duplicate-event tests | Frontend |
| **#41** | Public deck listing/search/preview API | Backend |
| **#42** | Clone deck API, ownership + moderation | Backend |
| **#43** | Public gallery và share UI | Frontend |
| **#53** | Backend unit/integration test suite | Test |
| **#54** | Frontend component/e2e test suite | Test |
