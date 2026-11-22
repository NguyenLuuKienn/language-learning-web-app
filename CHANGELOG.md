# Nhật ký Thay đổi Phiên bản (CHANGELOG)

Tất cả những thay đổi đáng chú ý của dự án sẽ được ghi nhận tại tài liệu này theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) và tuân thủ [Semantic Versioning](https://semver.org/).

---

## [v1.0.0-local] - 2026-10-17

### Added
- **Curriculum & Language Management with Import/Export (Admin Center)**:
  - Language entity extended with `NativeName` and `FlagEmoji`.
  - Course entity extended with `Domain` (General, IT, Business, Travel, Academic).
  - Lesson entity extended with `LessonType` (Vocabulary, Writing, SpeakingReading, Quiz).
  - Granular CRUD endpoints for languages (`/api/v1/admin/languages`) with dedicated request/response DTOs.
  - 1-Click curriculum export (`/api/v1/admin/curriculum/export`) and batch import (`/api/v1/admin/curriculum/import`) for rapid JSON seeding.
  - Admin UI tab for curriculum inspection, language management, and JSON import/export modal with sample template download.

- **Onboarding Questionnaire & Personalized Learning**:
  - Modal questionnaire (`StudyTargetModal`) prompting first-time users for Target Language, Proficiency Level (A1-C1), and Specialized Domain before entering the site.
  - Persistent target storage in `localStorage` (`study_target`) with quick switcher accessible from the header and sidebar.
  - Dedicated Home Dashboard (`HomeView`) set as the default landing view, showing target card, streak status, cards due, and learning shortcuts.

- **Multi-Skill Specialized Practice Modules**:
  - Topic-Based Writing Practice (`TopicWritingView`): domain-tailored scenarios (e.g. IT hotfix notice, PR review, partnership proposals) with live word/char counters, keyword inserters, and instant evaluation.
  - Speaking & Reading Practice (`SpeakingReadingView`): Web Speech API speech synthesis playback and browser speech recognition evaluation with real-time similarity scoring.

- **UI & Layout Enhancements**:
  - Global typography scaled up by 20% (`html { font-size: 120%; }`).
  - Left navigation sidebar widened by 10% to 264px (`w-[264px]`).
  - Main content container expanded to 90% viewport width (`w-[90%] max-w-[95%] mx-auto`) with tight, compact component spacing.
  - Complete migration to Ant Design design system: replaced all raw emojis and unicode symbols with standard `@ant-design/icons`.
  - Upgraded all user notifications and confirmation dialogs to Ant Design (`antd` `message` and `Modal.confirm`), wrapped in `ConfigProvider` and `App` context.

### Security
- **Strict Role-Based Admin Protection**:
  - Backend controllers (`AdminCoursesController`, `AdminLanguagesController`, `AdminCurriculumController`, `AdminUsersController`) strictly locked with `[Authorize(Roles = "Admin")]`.
  - Frontend sidebar and routing strictly restricted to authenticated users with `role === 'Admin'`; unauthorized attempts are redirected immediately to the Home view.

### Changed
- **Code Quality & Comment Hygiene**:
  - Stripped 100% of redundant comments, section banner dividers, step labels, and inline structural notes across Backend (.NET) and Frontend (React/TS).
  - Enforced 100% English codebase standard: eliminated all Vietnamese comments, scripts text, Dockerfile labels, and markup strings outside of the `vi.ts` dictionary.
  - Localized language selection dynamically via translation tokens (`common.english`, `common.vietnamese`).

---

## [v0.1.0-alpha] - 2026-10-13

### Added
- Khởi tạo cấu trúc dự án Monorepo (React PWA + ASP.NET Core Web API + MySQL).
- Thiết lập quy định mã nguồn mở: `README.md`, `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`.
- Thiết lập cấu hình môi trường `.editorconfig`, `.gitignore`, `.env.example`, `docker-compose.yml`.
- Thiết lập quy trình GitHub: GitHub Actions CI workflows, Issue Templates (Bug, Feature, Task), Pull Request Template, `CODEOWNERS`.
- Xây dựng bộ kịch bản tự động hóa 62 Issues, Labels và Milestones cho GitHub Projects.
