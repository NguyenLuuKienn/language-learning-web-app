# LANGUAGE LEARNING WEB APPLICATION

> **Tài liệu chính của dự án** — Đề tài môn Phát triển phần mềm mã nguồn mở  
> **Phiên bản tài liệu:** 1.0 | **Ngày:** 12/10/2026 | **Trạng thái:** Đề xuất / chờ nhóm phê duyệt  
> **Nhóm:** 4 thành viên (Leader, Maintainer, Core Developer, Contributor)  
> **Stack:** React + TypeScript · ASP.NET Core Web API (C#) · MySQL · EF Core · GUID · PWA  
> **Phạm vi:** 16/16 module đều bắt buộc trong MVP v1.0.0

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Bài toán thực tế và mục tiêu](#2-bài-toán-thực-tế-và-mục-tiêu)
3. [Phạm vi và kế hoạch phiên bản](#3-phạm-vi-và-kế-hoạch-phiên-bản)
4. [Đối tượng và phân quyền](#4-đối-tượng-và-phân-quyền)
5. [Yêu cầu chức năng chi tiết](#5-yêu-cầu-chức-năng-chi-tiết)
6. [Yêu cầu phi chức năng](#6-yêu-cầu-phi-chức-năng)
7. [Quy trình nghiệp vụ](#7-quy-trình-nghiệp-vụ)
8. [Công nghệ và kiến trúc](#8-công-nghệ-và-kiến-trúc)
9. [Thiết kế dữ liệu](#9-thiết-kế-dữ-liệu)
10. [Thiết kế REST API](#10-thiết-kế-rest-api)
11. [Thiết kế giao diện](#11-thiết-kế-giao-diện)
12. [Cấu trúc repository](#12-cấu-trúc-repository)
13. [Vai trò và phân chia công việc](#13-vai-trò-và-phân-chia-công-việc)
14. [Quy trình cộng tác GitHub](#14-quy-trình-cộng-tác-github)
15. [Backlog Issues đề xuất](#15-backlog-issues-đề-xuất)
16. [Milestones và tiến độ](#16-milestones-và-tiến-độ)
17. [Kiểm thử và tiêu chí nghiệm thu](#17-kiểm-thử-và-tiêu-chí-nghiệm-thu)
18. [CI/CD và triển khai](#18-cicd-và-triển-khai)
19. [Bảo mật và vận hành](#19-bảo-mật-và-vận-hành)
20. [Tài liệu mã nguồn mở](#20-tài-liệu-mã-nguồn-mở)
21. [Kịch bản demo và minh chứng](#21-kịch-bản-demo-và-minh-chứng)
22. [Rủi ro và phương án xử lý](#22-rủi-ro-và-phương-án-xử-lý)
23. [Định nghĩa hoàn thành và checklist](#23-định-nghĩa-hoàn-thành-và-checklist)
24. [Quyết định cần chốt](#24-quyết-định-cần-chốt)

---

## 1. Tổng quan

**Tên đề tài:** Xây dựng ứng dụng web học ngoại ngữ tích hợp Flashcards và Spaced Repetition.  
**Tên tiếng Anh:** Language Learning Web Application.  
**Loại hình:** Web app/PWA mã nguồn mở, phục vụ học theo khóa và tự học bằng bộ thẻ.  
**Nhóm:** 4 thành viên — Leader, Maintainer, Core Developer, Contributor.  
**Trạng thái:** Tài liệu đặc tả và kế hoạch triển khai; chưa khẳng định đã hoàn thành chức năng nào.

### Tầm nhìn
Một người học có thể bắt đầu bằng khóa học có sẵn, tự tạo bộ thẻ, học và ôn từ vựng bằng SRS, làm quiz, xem tiến độ, học khi mất mạng và đồng bộ khi có mạng trở lại. Quản trị viên có thể tạo và quản lý nội dung học.

### Định vị sản phẩm
Lấy cảm hứng từ trải nghiệm học theo bài của Duolingo, bộ thẻ của Quizlet và phương pháp ôn tập của Anki; không sao chép mã nguồn, thiết kế hay thương hiệu của các sản phẩm này. Sản phẩm cần có nội dung học thực tế và quy trình học hoàn chỉnh.

### Mục tiêu môn học
Chứng minh quy trình cộng tác mã nguồn mở qua GitHub Organization/Repository, Issues, Milestones, Projects, Branch, Commit, Pull Request, Code Review, CI/CD, Test, Release, LICENSE và tài liệu đóng góp.

---

## 2. Bài toán thực tế và mục tiêu

### Vấn đề
- Người học phân tán tài liệu giữa ứng dụng học theo bài, ghi chú, flashcards và file CSV.
- Học thuộc trong một buổi dễ quên nếu không có lịch ôn phù hợp.
- Thiếu theo dõi tiến độ thống nhất và khả năng học khi kết nối kém.
- Người học muốn tự tạo và chia sẻ bộ thẻ phù hợp với mục tiêu cá nhân.

### Mục tiêu đo lường
- Hoàn thành luồng từ đăng ký → chọn khóa → học bài → làm quiz → lưu tiến độ.
- Hoàn thành luồng tạo deck → thêm thẻ → ôn SRS → lên lịch → ôn lại.
- Có thể thực hiện ôn tập offline và đồng bộ kết quả khi online.
- Có thể nhập/xuất CSV và chia sẻ/sao chép deck công khai.
- Tất cả 16 module bắt buộc được nghiệm thu bằng test cases và demo.
- Mỗi thành viên có Issues, commits, PR và hoạt động review có thể truy vết.

### Đối tượng
Sinh viên, người tự học ngoại ngữ, giáo viên/biên soạn nội dung, quản trị viên. Phiên bản đầu cung cấp **nội dung tiếng Anh A1 mẫu** nhưng mô hình dữ liệu phải hỗ trợ nhiều ngôn ngữ.

---

## 3. Phạm vi và kế hoạch phiên bản

**QUYẾT ĐỊNH PHẠM VI: Toàn bộ 16 module dưới đây đều là MVP bắt buộc cho v1.0.0.** Không gọi các mục CSV, multimedia, thống kê, streak, sharing, offline hoặc synchronization là tính năng tùy chọn.

| Mã | Module bắt buộc | Kết quả tối thiểu để nghiệm thu |
|---|---|---|
| M01 | Authentication | Đăng ký, đăng nhập, refresh/logout, hồ sơ, phân quyền |
| M02 | Course Management | Ngôn ngữ, khóa, unit, lesson, quản trị nội dung |
| M03 | Vocabulary | Từ, nghĩa, phiên âm, ví dụ, tìm kiếm |
| M04 | Deck Management | CRUD deck cá nhân, tag, riêng tư/công khai |
| M05 | Flashcards | Thẻ trước/sau, sửa/xóa, học/lật thẻ |
| M06 | Spaced Repetition | Lịch ôn, đánh giá Again/Hard/Good/Easy, due cards |
| M07 | Quiz & Practice | Multiple choice, điền từ, ghép cặp, chấm điểm |
| M08 | Learning Progress | Lưu hoàn thành lesson, quiz attempts, lịch sử |
| M09 | Import/Export | CSV có validation, preview, báo lỗi dòng, xuất |
| M10 | Multimedia | Ảnh/âm thanh gắn từ và thẻ, upload/phát |
| M11 | Advanced Statistics | Dashboard, biểu đồ, retention, heatmap |
| M12 | Learning Streak | Chuỗi ngày học, daily goal, timezone |
| M13 | Deck Sharing | Public deck, tìm kiếm, xem trước, sao chép |
| M14 | Offline Learning | PWA, cache, học/ôn thẻ đã tải về |
| M15 | Data Synchronization | Queue offline, retry, idempotency, conflict handling |
| M16 | Admin Management | Quản trị user, course, lesson, vocabulary và nội dung công khai |

**Các mốc phát triển nội bộ**: v0.1.0 Foundation; v0.2.0 Learning Engine; v0.3.0 Extended Learning; v1.0.0 MVP Release. Mốc v0.x không phải bản MVP đầy đủ.

### Giới hạn để kiểm soát khối lượng
- Nội dung mẫu: ít nhất 1 ngôn ngữ (tiếng Anh), 1 khóa A1, 5 lessons, tối thiểu 50 từ và câu hỏi phù hợp.
- Offline bắt buộc cho deck/thẻ đã tải, lịch ôn, ghi nhận đánh giá và tiến độ liên quan; không bắt buộc tải mọi khóa hoặc phát trực tuyến khi offline.
- Đồng bộ bắt buộc cho thay đổi offline thuộc phạm vi trên; các xung đột phải có chính sách xác định và kiểm thử.
- Chia sẻ deck bằng public listing + clone/copy, chưa cần đồng biên tập thời gian thực.
- Multimedia bắt buộc upload/hiển thị ảnh và phát audio; không bắt buộc speech recognition hay AI.
- Hỗ trợ CSV chuẩn của sản phẩm; nhập `.apkg` không thuộc v1.0.0.

---

## 4. Đối tượng và phân quyền

| Quyền | Guest | Learner | Admin |
|---|:---:|:---:|:---:|
| Xem landing, catalog public | ✓ | ✓ | ✓ |
| Đăng ký/đăng nhập | ✓ | ✓ | ✓ |
| Học khóa, làm quiz, xem tiến độ riêng | — | ✓ | ✓ |
| CRUD deck/thẻ của mình | — | ✓ | ✓ |
| Đặt deck public, sao chép deck public | — | ✓ | ✓ |
| Quản lý khóa học chính thức | — | — | ✓ |
| Quản trị tài khoản, xử lý nội dung | — | — | ✓ |

**Phân biệt:** Vai trò GitHub (Leader/Maintainer/Core Developer/Contributor) là vai trò phát triển dự án, **không phải** vai trò quyền truy cập web app.

### Quy tắc
- Learner chỉ đọc/sửa dữ liệu riêng; deck công khai của người khác chỉ được xem hoặc sao chép.
- Không cho phép chỉnh trực tiếp deck gốc của người khác qua chức năng clone.
- Admin có quyền ẩn deck vi phạm; thao tác quản trị có audit log.
- API kiểm tra quyền phía server; ẩn nút UI không thay thế kiểm tra authorization.

---

## 5. Yêu cầu chức năng chi tiết

### M01 — Authentication & Profile
FR-001 đăng ký email, mật khẩu; FR-002 đăng nhập/đăng xuất; FR-003 refresh token có thu hồi; FR-004 xem/sửa hồ sơ; FR-005 phân quyền Learner/Admin; FR-006 đổi mật khẩu. Email phải duy nhất, mật khẩu được hash bằng thuật toán chuyên dụng; không lưu plaintext.

### M02 — Course Management
FR-010 danh sách ngôn ngữ; FR-011 catalog khóa học theo ngôn ngữ/cấp độ; FR-012 cấu trúc Course → Unit → Lesson; FR-013 mở lesson; FR-014 admin CRUD/publish/unpublish; FR-015 sắp thứ tự lesson và kiểm tra ràng buộc trước khi xóa.

### M03 — Vocabulary
FR-020 CRUD từ vựng theo lesson; FR-021 lưu ngôn ngữ, từ gốc, phiên âm, nghĩa, ví dụ, tag; FR-022 tìm kiếm/lọc; FR-023 gắn media; FR-024 không hiển thị nội dung unpublished cho learner.

### M04 — Deck Management
FR-030 CRUD deck; FR-031 gắn thẻ/tag, mô tả, ngôn ngữ; FR-032 trạng thái private/public; FR-033 thống kê tổng thẻ, thẻ mới, đến hạn; FR-034 chỉ chủ sở hữu được chỉnh sửa.

### M05 — Flashcards
FR-040 tạo/sửa/xóa card hai mặt; FR-041 lật thẻ; FR-042 gắn ảnh/audio; FR-043 học thẻ mới và ôn thẻ đến hạn; FR-044 lưu kết quả đánh giá; FR-045 hỗ trợ duyệt và tìm kiếm card trong deck.

### M06 — SRS
FR-050 chọn Again/Hard/Good/Easy; FR-051 lưu trạng thái và due time từng card theo user; FR-052 truy vấn thẻ đến hạn; FR-053 lịch sử review; FR-054 giới hạn số thẻ mới/ngày; FR-055 cùng một review offline không được áp dụng hai lần khi retry. Thuật toán được đóng gói sau `ISrsScheduler` để có thể thay đổi, kiểm thử và phiên bản hóa.

### M07 — Quiz
FR-060 multiple choice; FR-061 điền từ; FR-062 ghép cặp; FR-063 tính điểm phía server với dữ liệu online; FR-064 lưu attempts và answers; FR-065 hiển thị giải thích/đáp án sau khi nộp. Trường hợp quiz offline nếu hỗ trợ phải có quy tắc chấm và đồng bộ rõ ràng.

### M08 — Learning Progress
FR-070 lưu trạng thái chưa học/đang học/hoàn thành lesson; FR-071 lưu quiz attempts; FR-072 tiếp tục bài gần nhất; FR-073 tiến độ theo course; FR-074 không ghi nhận hoàn thành lặp do retry.

### M09 — CSV Import/Export
FR-080 tải template; FR-081 preview trước khi import; FR-082 kiểm tra encoding UTF-8, cột bắt buộc, dòng trùng/không hợp lệ; FR-083 import có báo số thành công/thất bại; FR-084 export CSV có escape đúng; FR-085 chống CSV formula injection khi xuất.

### M10 — Multimedia
FR-090 upload ảnh và audio; FR-091 giới hạn MIME/size và kiểm tra nội dung; FR-092 phát âm thanh, xem ảnh; FR-093 fallback khi media lỗi; FR-094 chỉ chủ sở hữu hoặc admin được thay/xóa media; FR-095 file offline chỉ sẵn có nếu đã cache/tải về.

### M11 — Advanced Statistics
FR-100 số lần review/ngày; FR-101 số từ mới/đã học/đến hạn; FR-102 tỷ lệ trả lời theo mức độ; FR-103 heatmap hoạt động; FR-104 biểu đồ theo ngày/tuần/tháng; FR-105 thống kê dựa trên dữ liệu đồng bộ, có chú thích nếu còn pending offline.

### M12 — Streak
FR-110 daily goal; FR-111 tính ngày học hợp lệ theo timezone người dùng; FR-112 cập nhật streak sau hoạt động hợp lệ; FR-113 tránh tăng streak hai lần do sự kiện trùng; FR-114 hiển thị lịch sử ngày đạt mục tiêu.

### M13 — Deck Sharing
FR-120 công khai/ẩn deck; FR-121 public catalog và search; FR-122 xem trước nội dung; FR-123 clone deck vào tài khoản; FR-124 lưu source deck id để ghi công nguồn; FR-125 admin ẩn deck vi phạm. Clone tạo bản độc lập, không tự đồng bộ nội dung với deck gốc.

### M14 — Offline Learning
FR-130 cài đặt PWA; FR-131 tải trước deck được chọn; FR-132 hiển thị trạng thái online/offline; FR-133 học và review deck đã tải khi offline; FR-134 dữ liệu cục bộ không bị mất khi refresh; FR-135 cảnh báo giới hạn dung lượng và trường hợp trình duyệt xóa cache.

### M15 — Data Synchronization
FR-140 lưu local operations trong IndexedDB; FR-141 sync khi có kết nối; FR-142 retry exponential backoff; FR-143 idempotency key; FR-144 phát hiện version conflict; FR-145 giải quyết conflict theo quy tắc từng loại dữ liệu; FR-146 hiển thị pending/synced/failed; FR-147 test offline → online và nhiều thiết bị.

### M16 — Admin Management
FR-150 danh sách user; FR-151 khóa/mở tài khoản theo chính sách; FR-152 CRUD/publish course, unit, lesson; FR-153 quản lý vocabulary/questions; FR-154 moderation public decks; FR-155 audit log thao tác nhạy cảm.

### Acceptance criteria mẫu
**AC-SRS-01:** Khi learner đánh giá một thẻ, hệ thống ghi đúng một review event và tính due time tiếp theo.  
**AC-OFFLINE-01:** Khi mất mạng, learner vẫn mở được deck đã tải, lật thẻ và chọn mức ghi nhớ.  
**AC-SYNC-01:** Khi có mạng trở lại, các review offline được đồng bộ đúng một lần dù request bị gửi lại.  
**AC-SHARE-01:** Clone public deck không làm thay đổi deck của chủ sở hữu.  
**AC-CSV-01:** CSV lỗi định dạng được báo dòng/cột; dữ liệu không hợp lệ không làm hỏng deck hiện tại.

---

## 6. Yêu cầu phi chức năng

| Mã | Yêu cầu | Cách kiểm chứng |
|---|---|---|
| NFR-01 | Responsive desktop/mobile | Test các breakpoint 360px, 768px, 1280px |
| NFR-02 | Accessibility cơ bản | Keyboard navigation, label, contrast, focus |
| NFR-03 | Bảo mật | Hash password, JWT validation, authorization, rate limit |
| NFR-04 | Hiệu năng | API danh sách phân trang; kiểm thử với dataset mẫu |
| NFR-05 | Tin cậy | Không mất dữ liệu review trong kịch bản offline/retry |
| NFR-06 | Dễ bảo trì | Layering, DI, logging, typed DTO, migrations |
| NFR-07 | Kiểm thử | Unit/integration cho logic quan trọng, CI xanh |
| NFR-08 | Khả năng triển khai | Docker Compose chạy được frontend/API/MySQL |
| NFR-09 | Quan sát lỗi | Structured logs, error handling, correlation ID |
| NFR-10 | Quốc tế hóa | UI có thể mở rộng đa ngôn ngữ; Unicode đầy đủ |

**Mục tiêu ban đầu để nhóm xác nhận:** các API CRUD phổ biến có p95 dưới 500 ms trong môi trường demo với dữ liệu mẫu, không tính thời gian tải media; không cam kết chỉ số production nếu chưa benchmark. Không lưu JWT lâu dài trong localStorage nếu có thể dùng mô hình cookie an toàn; lựa chọn auth cần được thống nhất và threat-model.

---

## 7. Quy trình nghiệp vụ

### Flow A — Học theo khóa
1. Learner đăng nhập và chọn ngôn ngữ/khóa học.
2. Mở unit và lesson, đọc nội dung/từ vựng.
3. Làm quiz, hệ thống chấm và lưu `QuizAttempt`.
4. Cập nhật `LessonProgress`, dashboard và streak theo quy tắc.
5. Tiếp tục lesson kế tiếp.

### Flow B — Tự tạo deck và ôn SRS
1. Learner tạo deck riêng và thêm flashcards.
2. Mở phiên học, hệ thống lấy cards mới và due cards.
3. Lật thẻ và chọn Again/Hard/Good/Easy.
4. Scheduler tính trạng thái và thời điểm ôn tiếp theo.
5. Lưu `ReviewEvent` và cập nhật `CardSchedule`.

### Flow C — Chia sẻ deck
1. Chủ sở hữu bật public.
2. Người khác tìm deck, xem trước, clone.
3. Deck clone có ID/owner mới, có liên kết nguồn nhưng dữ liệu độc lập.
4. Admin có thể ẩn deck công khai vi phạm.

### Flow D — Offline và đồng bộ
1. Learner tải deck và media cần thiết khi online.
2. App cache shell + dữ liệu đã chọn vào IndexedDB/Cache Storage.
3. Mất mạng: tạo review event với UUID/idempotency key, cập nhật UI local.
4. Khi online: queue gửi batch có thứ tự; server xác thực, bỏ qua event trùng.
5. Server trả accepted/conflict/rejected và trạng thái mới.
6. Client cập nhật local snapshot, giữ event lỗi để người dùng retry/xử lý.
7. Đăng xuất: xử lý dữ liệu offline theo chính sách bảo mật, không để lộ giữa tài khoản.

### Flow E — CSV
Upload → parse/validate → preview → confirm → import transaction/chunk → result report. Export phải hỗ trợ UTF-8, escaping và header chuẩn.

---

## 8. Công nghệ và kiến trúc

| Thành phần | Lựa chọn |
|---|---|
| Frontend | React + TypeScript + Vite |
| UI | Tailwind CSS |
| Routing | React Router |
| State/API | TanStack Query; state local tùy module |
| PWA | Service Worker + Web App Manifest (có thể dùng Vite PWA plugin) |
| Offline store | IndexedDB (có thể dùng Dexie) |
| Backend | C# ASP.NET Core Web API (phiên bản .NET LTS được nhóm chốt) |
| ORM | Entity Framework Core + Pomelo.EntityFrameworkCore.MySql tương thích |
| Database | MySQL 8.x |
| Keys | `Guid` trong C#; `CHAR(36)` nhất quán hoặc `BINARY(16)` nếu cấu hình chuyển đổi rõ |
| Auth | ASP.NET Core Identity hoặc auth service được kiểm thử + JWT/cookie |
| Testing | xUnit, integration tests, Vitest, React Testing Library, Playwright |
| API docs | OpenAPI / Swagger UI |
| CI/CD | GitHub Actions |
| Local deployment | Docker, Docker Compose |

### Kiến trúc logic
```text
React PWA
  ├─ Pages / Components / Features
  ├─ API Client / Auth
  ├─ IndexedDB (decks, cards, review queue)
  ├─ Service Worker (app shell, cached assets)
  └─ Sync Engine
            │ HTTPS REST/JSON
ASP.NET Core Web API
  ├─ Controllers / Endpoints
  ├─ Application Services (course, deck, quiz, SRS, sync)
  ├─ Domain Models + Business Rules
  ├─ Infrastructure (EF Core, media, auth)
  └─ Background tasks (nếu cần)
            │
        MySQL + Media Storage
```

### Kiến trúc backend
`Api → Application → Domain`, `Infrastructure` triển khai persistence và adapter. Không đưa business rules trực tiếp vào Controller. Các tác vụ đơn giản không cần repository wrapper phức tạp nếu EF Core DbContext đã đáp ứng.

### GUID
Tạo ID bằng `Guid.NewGuid()` (hoặc strategy khác được chốt) cho entity mới; database và JSON API nhất quán UUID dạng chuỗi. FK phải đồng kiểu. MySQL `UUID()` không bắt buộc nếu backend tạo ID. GUID không thay thế kiểm tra quyền truy cập.

---

## 9. Thiết kế dữ liệu

### Quy ước
- Bảng dùng tên `snake_case` hoặc `PascalCase` theo một quy ước thống nhất.
- `id` và các foreign key là GUID; `created_at`, `updated_at` UTC.
- `row_version`/`version` để phát hiện xung đột ở entity có sửa đổi.
- Index theo `user_id`, `deck_id`, `due_at`, `course_id`, `visibility`, `created_at`.
- Unique constraints cho email, progress theo user/lesson, schedule theo user/card và idempotency key theo user.

### Danh sách bảng cốt lõi
| Bảng | Các trường chính (ngoài id, timestamps) | Quan hệ / ghi chú |
|---|---|---|
| `users` | email, password_hash, display_name, role, timezone, status | Chủ sở hữu dữ liệu |
| `refresh_tokens` | user_id, token_hash, expires_at, revoked_at | Quản lý phiên |
| `languages` | code, name, is_active | 1–N courses |
| `courses` | language_id, title, level, description, status | 1–N units |
| `units` | course_id, title, sort_order | 1–N lessons |
| `lessons` | unit_id, title, content, sort_order, status | 1–N vocabulary/questions |
| `vocabularies` | lesson_id, term, phonetic, meaning, example, language_id | Có media |
| `decks` | owner_id, language_id, title, description, visibility, source_deck_id, version | 1–N cards |
| `deck_tags` | deck_id, tag | Có unique (deck_id, tag) |
| `cards` | deck_id, front, back, version | 1–N card media |
| `media_assets` | owner_id, storage_key, mime_type, size_bytes, kind | Metadata file |
| `card_media` | card_id, media_id, side | Gắn ảnh/audio |
| `vocabulary_media` | vocabulary_id, media_id | Media từ vựng |
| `card_schedules` | user_id, card_id, due_at, state_json, algorithm_version, last_reviewed_at | Unique(user_id, card_id) |
| `review_events` | user_id, card_id, rating, reviewed_at, client_event_id, algorithm_version, payload | Unique(user_id, client_event_id) |
| `questions` | lesson_id, type, prompt, explanation, config_json | Multiple choice/fill/matching |
| `question_options` | question_id, content, is_correct, sort_order | MCQ |
| `quiz_attempts` | user_id, lesson_id, score, max_score, completed_at | Lịch sử |
| `quiz_answers` | attempt_id, question_id, answer_json, is_correct | Đáp án |
| `lesson_progress` | user_id, lesson_id, status, completed_at, last_accessed_at | Unique(user_id, lesson_id) |
| `course_enrollments` | user_id, course_id, enrolled_at | Unique(user_id, course_id) |
| `daily_learning_stats` | user_id, local_date, timezone, review_count, lessons_completed, goal_met | Unique(user_id, local_date) |
| `user_goals` | user_id, daily_target, goal_type | Mục tiêu |
| `sync_operations` | user_id, client_operation_id, operation_type, entity_id, payload_hash, status, result_json | Idempotency |
| `audit_logs` | actor_id, action, target_type, target_id, metadata_json | Quản trị |

### Quan hệ trọng yếu
```text
languages 1---N courses 1---N units 1---N lessons
lessons 1---N vocabularies
lessons 1---N questions 1---N question_options
users 1---N decks 1---N cards
users N---N cards via card_schedules
users 1---N review_events
users N---N lessons via lesson_progress
users 1---N quiz_attempts 1---N quiz_answers
users 1---N sync_operations
```

### Quy tắc toàn vẹn
- Không xóa cứng deck có review lịch sử nếu chưa có chính sách; ưu tiên soft-delete hoặc archive.
- Card clone được tạo ID mới; schedule và review history không tự sao chép.
- `review_events` bất biến sau khi ghi; sửa lịch ôn phải qua event hoặc thao tác có audit.
- Dữ liệu quiz answer và SRS cần transaction ở những điểm cập nhật nhiều bảng.
- `daily_learning_stats` có thể là projection tái dựng được từ events, không coi là nguồn chân lý duy nhất.

---

## 10. Thiết kế REST API

Prefix `/api/v1`; yêu cầu đăng nhập dùng `Authorization: Bearer <token>` nếu chọn bearer JWT. Response lỗi dùng một cấu trúc nhất quán như Problem Details (`application/problem+json`).

| Method | Endpoint | Mục đích |
|---|---|---|
| POST | `/auth/register` | Đăng ký |
| POST | `/auth/login` | Đăng nhập |
| POST | `/auth/refresh` | Gia hạn phiên |
| POST | `/auth/logout` | Đăng xuất/thu hồi |
| GET/PATCH | `/me` | Hồ sơ |
| GET | `/languages` | Danh sách ngôn ngữ |
| GET | `/courses` | Catalog khóa học |
| GET | `/courses/{id}` | Chi tiết khóa |
| GET | `/courses/{id}/units` | Units |
| GET | `/lessons/{id}` | Chi tiết bài |
| POST | `/courses/{id}/enroll` | Ghi danh |
| GET | `/lessons/{id}/vocabulary` | Từ vựng |
| GET/POST | `/decks` | Danh sách/tạo deck |
| GET/PATCH/DELETE | `/decks/{id}` | Chi tiết/sửa/xóa deck |
| GET/POST | `/decks/{id}/cards` | Danh sách/tạo thẻ |
| PATCH/DELETE | `/cards/{id}` | Sửa/xóa thẻ |
| GET | `/reviews/due?deckId=...` | Thẻ đến hạn |
| POST | `/reviews` | Ghi đánh giá thẻ |
| GET | `/reviews/history` | Lịch sử ôn |
| GET | `/lessons/{id}/quiz` | Lấy đề |
| POST | `/quiz-attempts` | Nộp bài/chấm |
| GET | `/quiz-attempts` | Lịch sử quiz |
| GET/PATCH | `/progress/lessons/{id}` | Tiến độ lesson |
| POST | `/decks/{id}/imports/preview` | Preview CSV |
| POST | `/decks/{id}/imports` | Import CSV |
| GET | `/decks/{id}/export` | Export CSV |
| POST | `/media` | Upload ảnh/audio |
| GET | `/statistics/overview` | Dashboard |
| GET | `/statistics/heatmap` | Heatmap |
| GET/PATCH | `/me/daily-goal` | Mục tiêu học |
| GET | `/public/decks` | Deck công khai |
| POST | `/public/decks/{id}/clone` | Sao chép deck |
| POST | `/sync/push` | Đẩy operations offline |
| GET | `/sync/pull?cursor=...` | Kéo thay đổi server |
| GET | `/sync/status` | Trạng thái đồng bộ |
| GET/POST/PATCH | `/admin/courses` | Quản trị khóa |
| GET/PATCH | `/admin/users` | Quản trị tài khoản |
| GET/PATCH | `/admin/public-decks` | Moderation |

**Ví dụ POST `/api/v1/reviews`:**
```json
{
  "clientEventId": "f01a79e4-faf0-4f1c-83e2-ff874ea3d451",
  "cardId": "bd19a84e-b18d-4560-ae1c-53b4ef047ad2",
  "rating": "Good",
  "reviewedAt": "2026-10-12T02:00:00Z",
  "algorithmVersion": "srs-v1"
}
```
Server lấy `userId` từ auth, không tin `userId` client gửi; response trả `dueAt`, `scheduleState`, `eventId` và trạng thái idempotent.

**Ví dụ POST `/api/v1/sync/push`:**
```json
{
  "deviceId": "a2f38b65-27c4-43b6-83b2-72e5a6e8fce2",
  "operations": [
    {
      "operationId": "f01a79e4-faf0-4f1c-83e2-ff874ea3d451",
      "type": "review.submit",
      "entityId": "bd19a84e-b18d-4560-ae1c-53b4ef047ad2",
      "baseVersion": 3,
      "occurredAt": "2026-10-12T02:00:00Z",
      "payload": {"rating": "Good", "algorithmVersion": "srs-v1"}
    }
  ]
}
```
Kết quả từng operation: `accepted`, `duplicate`, `conflict`, `rejected`; không trả thành công cho toàn batch nếu có lỗi mà không ghi rõ từng phần.

### Phân trang và validation
- List endpoints dùng `page`, `pageSize` hoặc cursor; giới hạn `pageSize`.
- DTO kiểm tra dữ liệu, trả lỗi field-level; không trả stack trace ra client.
- OpenAPI là hợp đồng API; thay đổi breaking cần review.

---

## 11. Thiết kế giao diện

### Trang công khai
Landing, login/register, danh sách khóa, danh sách deck công khai, preview deck.

### Learner
Dashboard, course catalog, course detail, lesson viewer, vocabulary browser, quiz player/result, My Decks, deck editor, flashcard study, daily review, statistics, streak/calendar, import/export wizard, profile/settings, sync status/offline downloads.

### Admin
Admin dashboard, quản lý ngôn ngữ/khóa/unit/lesson, vocabulary/question editor, user management, public deck moderation, audit view.

### UX quan trọng
- Flashcard hỗ trợ bàn phím (Space lật, phím chọn rating), focus rõ ràng.
- Màn hình offline thể hiện nội dung nào đã tải và số thao tác chờ đồng bộ.
- Không giả vờ đã sync khi đang offline; thông báo lỗi có retry.
- CSV import hiển thị preview, số dòng lỗi và hành động xác nhận.
- Statistics phân biệt dữ liệu đã đồng bộ và pending local.
- Responsive và trạng thái loading/empty/error đầy đủ.

### Danh sách wireframe cần thiết
`WF-01 Login`, `WF-02 Dashboard`, `WF-03 Course Detail`, `WF-04 Lesson`, `WF-05 Quiz`, `WF-06 My Decks`, `WF-07 Deck Editor`, `WF-08 Flashcard Review`, `WF-09 Statistics`, `WF-10 Public Decks`, `WF-11 Import CSV`, `WF-12 Sync Center`, `WF-13 Admin Content`.

---

## 12. Cấu trúc repository

Khuyến nghị **monorepo**:
```text
language-learning-web-app/
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── app/
│   │   ├── components/
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   ├── courses/
│   │   │   ├── decks/
│   │   │   ├── reviews/
│   │   │   ├── quiz/
│   │   │   ├── statistics/
│   │   │   └── sync/
│   │   ├── lib/
│   │   └── offline/
│   ├── package.json
│   └── vite.config.ts
├── backend/
│   ├── LanguageLearning.Api/
│   ├── LanguageLearning.Application/
│   ├── LanguageLearning.Domain/
│   ├── LanguageLearning.Infrastructure/
│   ├── LanguageLearning.Tests/
│   └── LanguageLearning.sln
├── docs/
│   ├── architecture/
│   ├── api/
│   ├── database/
│   ├── decisions/
│   └── screenshots/
├── .github/
│   ├── workflows/
│   ├── ISSUE_TEMPLATE/
│   ├── CODEOWNERS
│   └── PULL_REQUEST_TEMPLATE.md
├── docker-compose.yml
├── .env.example
├── .editorconfig
├── README.md
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── SECURITY.md
├── CHANGELOG.md
└── LICENSE
```
Không commit `.env`, mật khẩu, connection strings thật, tokens, file media chứa dữ liệu riêng hoặc `node_modules`.

---

## 13. Vai trò và phân chia công việc

| Vai trò GitHub | Module chính | Trách nhiệm quy trình |
|---|---|---|
| Leader | Authentication, Profile, Statistics, Streak | Roadmap, GitHub Projects, phân công, acceptance, điều phối |
| Maintainer | Courses, Lessons, Vocabulary, Admin | Review, bảo vệ nhánh, CI/CD, release, chuẩn coding |
| Core Developer | Decks, Flashcards, SRS, Multimedia | Thiết kế domain/database/API, test logic, review kỹ thuật |
| Contributor | Quiz, Progress, CSV, Sharing, Offline/Sync UI | Feature PR, test, tài liệu, demo trải nghiệm |

**Offline/Sync là workstream liên chức năng**: Core Developer phụ trách hợp đồng server/idempotency; Contributor phụ trách IndexedDB/queue UI; Maintainer phụ trách integration/CI; Leader phụ trách acceptance và test nhiều thiết bị. Không giao toàn bộ offline sync cho một người.

### Nguyên tắc công bằng
- Mỗi thành viên có tối thiểu 1 feature xuyên suốt frontend → API → DB hoặc integration rõ ràng.
- Mỗi người có PR được review bởi người khác; tránh tự approve.
- Leader và Maintainer vẫn phải có đóng góp code/tài liệu kỹ thuật có thể chứng minh.
- Issue assignment là trách nhiệm chính, người khác có thể hỗ trợ qua linked PR.
- Ưu tiên review chéo; người tạo PR không tự merge nếu quy định branch protection yêu cầu approval.

---

## 14. Quy trình cộng tác GitHub

### Branching
- `main`: ổn định, tag/release; protected.
- `develop`: tích hợp; protected nếu gói GitHub hỗ trợ.
- `feature/<issue>-<slug>`, `fix/<issue>-<slug>`, `docs/<issue>-<slug>`, `test/<issue>-<slug>`.
- Feature branch từ `develop`, PR về `develop`; release PR từ `develop` vào `main`.

### Workflow Issue → PR
1. Leader/Maintainer tạo Issue có mô tả, acceptance criteria, labels, assignee, milestone.
2. Thành viên tạo branch theo Issue.
3. Commit theo Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `chore:`).
4. Push và mở PR, liên kết `Closes #123` (tùy target branch và cách GitHub đóng issue; kiểm tra khi merge).
5. CI chạy lint/build/test; reviewer yêu cầu sửa hoặc approve.
6. Author cập nhật, reviewer xác nhận, merge.
7. QA/demo nghiệm thu; đóng Issue và cập nhật Project.
8. Release với changelog và tag.

### Labels đề xuất
`type:feature`, `type:bug`, `type:docs`, `type:test`, `type:chore`, `priority:P0`, `priority:P1`, `priority:P2`, `area:frontend`, `area:backend`, `area:database`, `area:offline`, `area:ci`, `status:blocked`, `good-first-issue`.

### GitHub Projects
Các cột `Backlog → Ready → In Progress → In Review → Testing → Done`; thêm trường `Owner`, `Priority`, `Milestone`, `Estimate`, `Module`.

### PR template tối thiểu
- Summary / Linked issue
- Screenshots/API changes
- Test evidence
- DB migration?
- Offline/sync impact?
- Security/privacy impact?
- Checklist build/test/docs

### CODEOWNERS gợi ý
- `/backend/` Core Developer + Maintainer
- `/frontend/src/offline/` Contributor + Core Developer
- `/.github/` Maintainer + Leader
Không phụ thuộc CODEOWNERS để thay thế review thực tế; kiểm tra giới hạn quyền theo loại repository và plan.

---

## 15. Backlog Issues đề xuất

**Backlog ban đầu: 62 Issues**; mỗi Issue phải bổ sung mô tả, acceptance criteria, estimate, dependency và test evidence khi đưa vào GitHub. Đây là danh sách đề xuất, không phải Issue đã tạo.

| # | Workstream | Issue đề xuất | Owner chính |
|---:|---|---|---|
| 01 | Foundation | Khởi tạo GitHub Organization/monorepo, README và LICENSE | Leader |
| 02 | Foundation | Thiết lập GitHub Projects, labels, milestones, issue/PR templates | Leader |
| 03 | Foundation | Cấu hình frontend React/TS/Tailwind/router | Contributor |
| 04 | Foundation | Khởi tạo ASP.NET Core solution và DI layers | Core Developer |
| 05 | Foundation | Cấu hình MySQL, EF Core provider và migrations | Core Developer |
| 06 | Foundation | Docker Compose + env.example + seed | Maintainer |
| 07 | Foundation | Thiết lập CI frontend/backend | Maintainer |
| 08 | Foundation | Thiết kế ERD và ADR GUID/UUID | Core Developer |
| 09 | Foundation | Thiết kế OpenAPI conventions, Problem Details | Maintainer |
| 10 | Auth | API đăng ký, đăng nhập, hash mật khẩu | Leader |
| 11 | Auth | JWT/refresh/logout và authorization | Leader |
| 12 | Auth | Frontend auth screens + protected routes | Leader |
| 13 | Auth | User profile + timezone settings | Leader |
| 14 | Courses | CRUD languages/courses/units/lessons admin | Maintainer |
| 15 | Courses | Publish/unpublish, ordering, validation | Maintainer |
| 16 | Courses | Course catalog và course detail UI | Maintainer |
| 17 | Courses | Lesson viewer và navigation | Maintainer |
| 18 | Vocabulary | Vocabulary CRUD + search/filter | Maintainer |
| 19 | Vocabulary | Vocabulary UI và nội dung mẫu A1 | Maintainer |
| 20 | Decks | Deck CRUD, tags, visibility API | Core Developer |
| 21 | Decks | My Decks UI, editor và search | Core Developer |
| 22 | Flashcards | Card CRUD API và validations | Core Developer |
| 23 | Flashcards | Flashcard editor, flip animation, shortcuts | Core Developer |
| 24 | SRS | Đặc tả SRS algorithm, tests và versioning | Core Developer |
| 25 | SRS | Card schedule, due query, review events | Core Developer |
| 26 | SRS | Review session UI và rating buttons | Core Developer |
| 27 | SRS | Idempotent review submission và concurrency tests | Core Developer |
| 28 | Quiz | Question models/API multiple choice | Contributor |
| 29 | Quiz | Fill-in-blank và matching question types | Contributor |
| 30 | Quiz | Quiz UI, score, results và history | Contributor |
| 31 | Progress | Lesson progress + enrollments API | Contributor |
| 32 | Progress | Resume learning + progress UI | Contributor |
| 33 | CSV | CSV template và import preview/validation | Contributor |
| 34 | CSV | CSV import commit + export + security tests | Contributor |
| 35 | Media | Media storage abstraction, upload validation | Core Developer |
| 36 | Media | Image/audio preview và playback UI | Core Developer |
| 37 | Statistics | Review/day, retention, heatmap API | Leader |
| 38 | Statistics | Dashboard charts và filters | Leader |
| 39 | Streak | Daily goal/timezone/streak calculation | Leader |
| 40 | Streak | Streak UI, calendar, duplicate-event tests | Leader |
| 41 | Sharing | Public deck listing/search/preview API | Contributor |
| 42 | Sharing | Clone deck API, ownership + moderation | Contributor |
| 43 | Sharing | Public gallery và share UI | Contributor |
| 44 | Offline | PWA manifest, service worker, app shell cache | Contributor |
| 45 | Offline | IndexedDB schema và deck predownload | Contributor |
| 46 | Offline | Offline review queue + pending UI | Contributor |
| 47 | Sync | Sync push endpoint + idempotency ledger | Core Developer |
| 48 | Sync | Sync pull cursor, entity versions, conflicts | Core Developer |
| 49 | Sync | Client retry/backoff/reconciliation | Contributor |
| 50 | Sync | Offline→online integration test, multi-device conflict | Maintainer |
| 51 | Admin | Admin user management và moderation | Maintainer |
| 52 | Admin | Admin course/content editor UI | Maintainer |
| 53 | Testing | Backend unit/integration test suite | Core Developer |
| 54 | Testing | Frontend component/e2e test suite | Contributor |
| 55 | Testing | Security and access-control tests | Maintainer |
| 56 | Testing | CSV, SRS, streak, offline/sync acceptance test | Leader |
| 57 | Delivery | Seed A1: 5 lessons, 50+ words, quiz content | Leader |
| 58 | Delivery | Deploy staging, smoke test, health checks | Maintainer |
| 59 | Delivery | Production deployment + backups/restore drill | Maintainer |
| 60 | Delivery | Release notes, screenshots, demo script | Leader |
| 61 | Delivery | Contribution guide, code of conduct, security policy | Leader |
| 62 | Delivery | v1.0.0 release, final retrospective | Leader |

### Phụ thuộc quan trọng
- Database/auth trước deck/course/progress.
- Card CRUD trước SRS.
- SRS event schema và idempotency trước sync.
- IndexedDB/Service Worker trước offline acceptance.
- Quiz attempt trước progress/statistics.
- Public deck/ownership trước clone.
- Release chỉ khi tất cả 16 module có acceptance evidence.

---

## 16. Milestones và tiến độ

| Milestone | Phạm vi | Điều kiện hoàn thành |
|---|---|---|
| `v0.1.0 Foundation` | Repo, CI, DB, auth, courses, vocabulary | Có thể đăng nhập, học nội dung mẫu; CI chạy |
| `v0.2.0 Learning Engine` | Decks, cards, SRS, quiz, progress, media | Hoàn thành học/ôn online |
| `v0.3.0 Extended Learning` | CSV, statistics, streak, sharing, offline, sync, admin | Toàn bộ module được tích hợp |
| `v1.0.0 MVP Release` | Hardening, tests, docs, deployment | Đạt checklist nghiệm thu 16 module |

### Lịch trình tham khảo 10 tuần (điều chỉnh theo lịch môn học)
| Tuần | Công việc |
|---|---|
| 1 | Phân tích, GitHub setup, ERD, kiến trúc, CI |
| 2 | Auth, user profile, course schema, seed |
| 3 | Course UI, vocabulary, admin content |
| 4 | Decks, flashcards, media |
| 5 | SRS + tests, quiz |
| 6 | Progress, CSV, statistics, streak |
| 7 | Sharing, public catalog, moderation |
| 8 | PWA, IndexedDB, offline review |
| 9 | Sync, conflict, integration/e2e/security tests |
| 10 | Fix bugs, docs, release, demo |

**Rủi ro lịch:** 16 module trong 10 tuần với 4 người là phạm vi lớn. Cần chốt capacity thực tế và ưu tiên hoàn thành luồng dọc (vertical slice) sớm. Không loại module khỏi MVP nếu chưa có quyết định thay đổi phạm vi của nhóm.

---

## 17. Kiểm thử và tiêu chí nghiệm thu

### Test pyramid
- **Unit:** SRS scheduler, streak timezone, CSV parser, quiz scoring, deck permissions.
- **Integration:** EF Core/MySQL migrations, auth, API, transaction, sync idempotency.
- **Frontend:** component tests, form validation, loading/error/empty states.
- **E2E:** đăng nhập→học→quiz; deck→review; offline→online; public deck→clone.
- **Manual:** responsive, keyboard, media playback, cross-browser/PWA.

### Bộ test case bắt buộc
| ID | Kịch bản | Kết quả mong đợi |
|---|---|---|
| TC-01 | Email trùng khi đăng ký | Bị từ chối có lỗi rõ |
| TC-02 | Learner gọi admin API | 403 |
| TC-03 | Learner sửa deck của người khác | 403/404 theo chính sách |
| TC-04 | Admin publish course | Learner xem được |
| TC-05 | Quiz MCQ/fill/matching | Chấm và lưu đúng |
| TC-06 | SRS Again/Hard/Good/Easy | Lịch phù hợp thuật toán phiên bản |
| TC-07 | Review event gửi hai lần | Chỉ một lần được áp dụng |
| TC-08 | CSV có dòng lỗi | Preview và báo lỗi chính xác |
| TC-09 | CSV chứa công thức | Export an toàn |
| TC-10 | Upload file không hợp lệ | Từ chối |
| TC-11 | Public deck được clone | ID/owner mới, độc lập |
| TC-12 | Private deck không truy cập công khai | Bị chặn |
| TC-13 | Streak qua ranh giới ngày/timezone | Tính đúng |
| TC-14 | Offline với deck đã tải | Học/ôn được |
| TC-15 | Offline refresh trang | Queue không mất |
| TC-16 | Reconnect và retry | Không tạo review trùng |
| TC-17 | Hai thiết bị cùng sửa deck | Conflict xử lý theo policy |
| TC-18 | Thay đổi mật khẩu/logout | Session xử lý đúng |
| TC-19 | Media cache chưa có khi offline | Fallback rõ |
| TC-20 | Admin ẩn deck vi phạm | Không xuất hiện catalog |

### Definition of Done
Issue chỉ Done khi code đã merge qua PR, CI xanh, có test tương ứng, migration/documentation được cập nhật, review approval hợp lệ, không lộ secret và acceptance criteria được xác nhận. Test failing phải có Issue xử lý, không bỏ qua.

---

## 18. CI/CD và triển khai

### CI — Pull Request
1. Checkout, setup .NET/Node.
2. Restore/install dependency với lockfile.
3. Lint/typecheck frontend, `dotnet format --verify-no-changes` nếu đã chuẩn hóa.
4. Build frontend/backend.
5. Unit tests.
6. Integration tests với MySQL service/container.
7. Kiểm tra migrations và test artifacts.
8. Optional: dependency/security scan và Playwright smoke.

### CD — Release
Merge `develop` → `main` qua PR; tag `v1.0.0`; build Docker images; triển khai staging/production theo môi trường; chạy migrations có backup; health check; publish release notes.

### Docker Compose local
- `mysql`: volume dữ liệu, healthcheck, credentials qua env.
- `api`: ASP.NET Core, đợi MySQL healthy, migrations qua bước có kiểm soát.
- `web`: build/static serving hoặc Vite dev service tùy profile.
- Optional object storage phù hợp media; local volume chỉ dành cho demo.

### Môi trường
`development`, `test`, `staging`, `production`. `.env.example` chỉ chứa placeholder. Secrets qua GitHub Secrets/hosting secrets; không commit khóa.

### Khôi phục
Backup MySQL và media metadata; diễn tập restore; chú ý media thực tế nếu dùng object storage. Không chạy destructive migrations tự động mà không kiểm tra.

---

## 19. Bảo mật và vận hành

- Dùng HTTPS production; CORS whitelist origin.
- Password hashing chuyên dụng, rate limit login, account lockout hợp lý.
- JWT signature/expiry/audience/issuer được kiểm tra; refresh token lưu hash và có revoke.
- Kiểm tra quyền sở hữu trên mọi deck/card/progress/media endpoint.
- Validate upload: MIME, signature, size, filename, storage key; tránh public bucket tùy tiện.
- CSV import giới hạn kích thước, số dòng; CSV export chống formula injection.
- Giới hạn payload sync, chống replay qua idempotency và user scoping.
- Chỉ đồng bộ dữ liệu thuộc user đã xác thực; không tin `userId` trong payload.
- Client offline lưu dữ liệu cá nhân: có logout cleanup, cache namespace theo user, tránh shared-device leakage.
- Không lưu password/token nhạy cảm trong IndexedDB.
- Thu thập dữ liệu học tập tối thiểu; có chính sách quyền riêng tư, xóa tài khoản và retention được chốt.
- Log không ghi mật khẩu, token, câu trả lời nhạy cảm hoặc nội dung riêng tư quá mức.
- Có `SECURITY.md`, quy trình báo lỗ hổng và cập nhật dependencies.

---

## 20. Tài liệu mã nguồn mở

| File | Nội dung |
|---|---|
| `README.md` | Mục tiêu, demo, stack, quick start, screenshots |
| `LICENSE` | Giấy phép OSS được nhóm chọn |
| `CONTRIBUTING.md` | Setup, branch, commit, PR, review, coding standards |
| `CODE_OF_CONDUCT.md` | Quy tắc cộng đồng |
| `SECURITY.md` | Báo cáo lỗ hổng và phạm vi hỗ trợ |
| `CHANGELOG.md` | Thay đổi theo release |
| `docs/architecture/` | Sơ đồ hệ thống, quyết định kỹ thuật |
| `docs/database/` | ERD, schema, migrations |
| `docs/api/` | OpenAPI, ví dụ requests |
| `docs/decisions/` | ADR: GUID, auth, SRS, offline conflict |
| `.github/ISSUE_TEMPLATE/` | Bug report, feature request, task |
| `.github/PULL_REQUEST_TEMPLATE.md` | PR checklist |

**Giấy phép:** Có thể cân nhắc MIT hoặc Apache-2.0, nhưng nhóm phải kiểm tra giấy phép dependencies, media, bộ từ vựng và nội dung khóa học. Không tự ý sao chép nội dung/âm thanh có bản quyền từ Anki, Duolingo hoặc nguồn khác. Dữ liệu mẫu nên tự biên soạn hoặc dùng nguồn được phép và ghi attribution.

---

## 21. Kịch bản demo và minh chứng

### Demo sản phẩm (15–20 phút)
1. Đăng nhập Learner; xem dashboard/streak.
2. Mở course A1, lesson, học vocabulary và làm quiz.
3. Tạo deck, thêm card có ảnh/audio, import CSV, export CSV.
4. Ôn flashcard, chọn rating, xem due date và thống kê.
5. Public deck, đăng nhập tài khoản khác và clone deck.
6. Tải deck để offline, ngắt mạng, ôn vài thẻ, refresh.
7. Kết nối lại, xem sync queue hoàn thành; xác nhận không trùng review.
8. Admin đăng nhập, chỉnh nội dung khóa và moderation.

### Demo GitHub (8–10 phút)
- GitHub Projects: Issue ở các trạng thái.
- Mở một Issue có acceptance criteria.
- Branch + commit + PR liên kết Issue.
- CI checks, reviewer comment, author sửa, approval, merge.
- Migrations/test evidence, releases/tags và CHANGELOG.
- GitHub Insights/Contributors để thấy đóng góp 4 thành viên.

### Bằng chứng cần lưu
Ảnh Projects board, issue detail, PR review conversation, Actions xanh, tests, release page, deployment, ERD, API docs, demo offline/sync và log thử nghiệm không chứa dữ liệu nhạy cảm.

---

## 22. Rủi ro và phương án xử lý

| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| 16 module vượt capacity | Cao | Vertical slice, weekly milestones, quản lý scope nội bộ |
| Offline sync mất/trùng review | Rất cao | Immutable events, idempotency, retry tests |
| SRS sai logic/không nhất quán | Cao | Scheduler versioning, unit tests, clock abstraction |
| Conflict đa thiết bị | Cao | Versioning, policy per entity, conflict tests |
| Media tốn storage/băng thông | Vừa | Giới hạn size, cache có chọn lọc |
| CSV nhập dữ liệu lỗi | Vừa | Preview, validation, transaction |
| Nội dung học có bản quyền | Cao | Tự biên soạn/nguồn mở có attribution |
| Thành viên ít đóng góp | Cao | Issue ownership, review chéo, weekly report |
| CI/CD thiếu ổn định | Vừa | Local parity qua Docker, test trên PR |
| Lộ secrets | Cao | `.gitignore`, secrets store, scan |
| Chậm tiến độ | Cao | Theo dõi burndown, báo blocker sớm |

---

## 23. Định nghĩa hoàn thành và checklist

### Product checklist
- [ ] M01 Authentication
- [ ] M02 Courses
- [ ] M03 Vocabulary
- [ ] M04 Decks
- [ ] M05 Flashcards
- [ ] M06 SRS
- [ ] M07 Quiz
- [ ] M08 Progress
- [ ] M09 CSV Import/Export
- [ ] M10 Image/Audio
- [ ] M11 Advanced Statistics
- [ ] M12 Streak
- [ ] M13 Deck Sharing
- [ ] M14 Offline
- [ ] M15 Synchronization
- [ ] M16 Admin
- [ ] Có dữ liệu khóa học mẫu, demo chạy end-to-end
- [ ] Offline→online không mất/trùng review

### Engineering checklist
- [ ] EF Core migrations + MySQL
- [ ] GUID consistent across PK/FK/API
- [ ] Unit/integration/e2e tests
- [ ] CI pass trên PR
- [ ] Docker Compose + `.env.example`
- [ ] OpenAPI và ERD cập nhật
- [ ] Logging/error handling/validation
- [ ] Auth, authorization, upload và sync security reviewed

### GitHub/OSS checklist
- [ ] Organization/repository public (nếu được phép)
- [ ] README, LICENSE, CONTRIBUTING, CODE_OF_CONDUCT, SECURITY
- [ ] GitHub Projects, Issues, Milestones, labels
- [ ] Branch strategy và branch protection
- [ ] PR review có lịch sử sửa đổi
- [ ] Mỗi thành viên có đóng góp và review
- [ ] Tags/releases/changelog
- [ ] Demo video/screenshots và báo cáo

---

## 24. Quyết định cần chốt

Các lựa chọn dưới đây **chưa được nhóm phê duyệt**, không xem là yêu cầu đã khóa:
1. Tên thương hiệu/app và tên GitHub Organization/Repository.
2. Phiên bản .NET LTS, MySQL, EF Core/Pomelo tương thích.
3. GUID storage: `CHAR(36)` (dễ debug) hay `BINARY(16)` (gọn hơn).
4. Cơ chế auth: Identity + bearer/refresh hoặc cookie secure; CSRF policy tương ứng.
5. Thuật toán SRS cụ thể: bản giản lược có test hay FSRS có thư viện/giấy phép phù hợp.
6. Quy tắc rating và learning steps; timezone, daily goal, streak reset.
7. Quy tắc conflict: review event append-only/idempotent; deck/card optimistic concurrency và UI xử lý xung đột.
8. Chính sách offline downloads, giới hạn dung lượng, retention và logout cleanup.
9. Nơi lưu media, quota, backup và deployment.
10. Giấy phép mã nguồn, quyền dùng dữ liệu từ vựng và media.
11. Thời lượng môn học, lịch họp nhóm, deadline và capacity mỗi thành viên.
12. Quy tắc release/branch protection thực tế theo quyền của repository.

### Thứ tự ra quyết định
Ưu tiên ADR cho (1) dữ liệu/GUID, (2) auth, (3) SRS, (4) offline/sync, (5) media; những quyết định này ảnh hưởng nhiều module và cần chốt sớm.
