# Language Learning (LinguaFlow)

Ứng dụng web học ngoại ngữ, được xây dựng trong học phần Phát triển phần mềm mã nguồn mở. Người học chọn ngôn ngữ, trình độ và lĩnh vực, học theo từng bài trong khóa học, sau đó ôn từ vựng bằng flashcard theo lịch lặp lại ngắt quãng (SRS).

Dự án gồm giao diện React, API ASP.NET Core và cơ sở dữ liệu MySQL. Nội dung học do quản trị viên biên soạn hoặc nhập từ JSON; bản cài đặt mới chưa có sẵn khóa học để học ngay.

## Chức năng

- **Tài khoản và mục tiêu học:** đăng ký, đăng nhập, cập nhật hồ sơ, chọn ngôn ngữ, trình độ, lĩnh vực và mục tiêu hằng ngày; phân quyền người học và quản trị viên.
- **Khóa học và bài học:** tổ chức theo khóa học → chương → bài; học theo các bước có điều kiện hoàn thành và lưu tiến độ để tiếp tục học.
- **Học từ vựng:** luyện từ theo phiên học, nghe phát âm, ghép cặp và điền từ. Từ đã hoàn thành được đưa vào lịch ôn SRS riêng của người học.
- **Bộ thẻ và ôn tập:** quản lý bộ thẻ cá nhân; ôn thẻ đến hạn và đánh giá mức độ nhớ bằng Again, Hard, Good, Easy; nhập/xuất CSV; chia sẻ và sao chép bộ thẻ công khai.
- **Luyện tập và kiểm tra:** bài nghe, nói, đọc, viết, ngữ pháp và quiz theo nội dung khóa học; bài kiểm tra đầu vào, luyện thi và luyện phỏng vấn khi có dữ liệu tương ứng.
- **Theo dõi học tập:** tiến độ bài học, kết quả luyện tập, thống kê, chuỗi ngày học và ghi chú cá nhân.
- **Quản trị:** quản lý người dùng, ngôn ngữ, phân loại khóa học, giáo trình; nhập/xuất JSON, kiểm tra chất lượng nội dung và duyệt, chấm các bài cần đánh giá thủ công.
- **Đồng bộ dữ liệu:** lưu bộ thẻ và hàng đợi thao tác bằng IndexedDB; có màn hình đồng bộ với API.

Một số giới hạn hiện tại:

- Chưa có manifest và service worker để cài đặt như PWA hoặc mở ứng dụng hoàn toàn ngoại tuyến. IndexedDB và API đồng bộ hiện có chưa thay thế được phần này.
- Màn hình hội thoại sử dụng mẫu câu trong giáo trình và giọng đọc của trình duyệt, chưa tích hợp dịch vụ AI hội thoại.
- Phát âm, nhận dạng giọng nói và ghi âm phụ thuộc trình duyệt, quyền micro và giọng đọc trên thiết bị. Bài luyện thi không cấp điểm IELTS/TOEIC chính thức; phần nói, viết cần chấm thủ công theo cấu hình đề.

## Công nghệ và kiến trúc

| Thành phần | Công nghệ |
| --- | --- |
| Giao diện | React 19, TypeScript 6, Vite 8, React Router 7 |
| UI | Ant Design 6, Tailwind CSS 3 |
| Dữ liệu phía trình duyệt | TanStack Query 5, Dexie 4 / IndexedDB |
| API | ASP.NET Core 8, Entity Framework Core 8, Swagger |
| Cơ sở dữ liệu | MySQL 8; có hỗ trợ SQLite và dùng SQLite in-memory trong kiểm thử tích hợp |
| Kiểm thử | xUnit, ASP.NET Core WebApplicationFactory, Playwright |
| Đóng gói và CI | Docker Compose, Nginx, GitHub Actions |

Backend tách thành bốn project: `Domain` chứa thực thể và quy tắc miền; `Application` chứa dịch vụ nghiệp vụ, DTO và interface; `Infrastructure` triển khai truy cập dữ liệu, xác thực và lưu tệp; `Api` cung cấp endpoint HTTP.

## Chạy bằng Docker Compose

Cần Git, Docker và Docker Compose. Thực hiện các lệnh dưới đây tại thư mục gốc của repository sau khi tải mã nguồn.

### 1. Cấu hình môi trường

Sao chép `.env.example` thành `.env`:

```powershell
Copy-Item .env.example .env
```

Trên Linux/macOS, dùng `cp .env.example .env`. Điền các giá trị sau trong `.env`:

| Biến | Ý nghĩa |
| --- | --- |
| `MYSQL_ROOT_PASSWORD` | Mật khẩu tài khoản root của MySQL |
| `MYSQL_DATABASE` | Tên database, mặc định `language_learning_db` |
| `MYSQL_USER`, `MYSQL_PASSWORD` | Tài khoản MySQL dành cho ứng dụng |
| `CONNECTION_STRING` | Chuỗi kết nối API tới MySQL trong Compose, dùng host `db` và cổng `3306` |
| `JWT_SECRET_KEY` | Khóa ký token, tối thiểu 32 byte UTF-8 |
| `BOOTSTRAP_ADMIN_PASSWORD` | Mật khẩu tạo tài khoản quản trị lần đầu |
| `MYSQL_PORT` | Cổng MySQL trên máy, mặc định `3306` |
| `FRONTEND_PORT` | Cổng web trên máy, mặc định `3000` |
| `ASPNETCORE_ENVIRONMENT` | Mặc định `Development`; Swagger chỉ bật trong môi trường này |

Ví dụ cấu trúc chuỗi kết nối; thay tài khoản và mật khẩu cho khớp các biến MySQL ở trên:

```dotenv
CONNECTION_STRING=Server=db;Port=3306;Database=language_learning_db;User=ll_user;Password=your_mysql_password;
```

File `.env` ở thư mục gốc được Docker Compose đọc. Các biến `API_PORT`, `JWT_EXPIRATION_MINUTES`, `REFRESH_TOKEN_EXPIRATION_DAYS` và `VITE_API_BASE_URL` trong file mẫu hiện không được Compose truyền vào dịch vụ tương ứng. API không có cổng truy cập trực tiếp từ máy trong cấu hình Compose hiện tại.

### 2. Khởi động

```sh
docker compose up --build -d
docker compose ps
```

Mở ứng dụng tại **http://localhost:3000**, hoặc cổng đã đặt trong `FRONTEND_PORT`. Nginx chuyển tiếp request `/api` tới backend; MySQL được mở tại `127.0.0.1:3306`, hoặc cổng đã đặt trong `MYSQL_PORT`.

Xem log và dừng các dịch vụ:

```sh
docker compose logs -f api
docker compose down
```

Database, tệp tải lên và khóa bảo vệ dữ liệu được lưu trong các Docker volume riêng, vẫn được giữ khi chạy `docker compose down`. Lệnh `docker compose down -v` sẽ xóa cả các volume này.

## Chạy để phát triển trên máy

Cần .NET SDK 8, Node.js 22.12 trở lên và npm, cùng MySQL 8. Có thể dùng MySQL cài trên máy hoặc chỉ khởi động dịch vụ database từ Compose sau khi cấu hình `.env`:

```sh
docker compose up -d db
```

### Backend

Ví dụ dưới đây dùng PowerShell, chạy từ thư mục gốc. Thay các giá trị minh họa bằng cấu hình của bạn:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Port=3306;Database=language_learning_db;User=ll_user;Password=your_mysql_password;'
$env:Jwt__SecretKey = 'replace-this-with-your-own-random-key-at-least-32-bytes'
$env:Bootstrap__AdminPassword = 'your_initial_admin_password'

dotnet restore backend/LanguageLearning.sln
dotnet run --project backend/src/LanguageLearning.Api --launch-profile http
```

API chạy tại **http://localhost:5039**; Swagger tại **http://localhost:5039/swagger**. Tiền tố API là `/api/v1`.

API chạy trực tiếp không tự đọc file `.env` ở thư mục gốc. Có thể cấu hình qua biến môi trường như trên hoặc dùng .NET User Secrets cho project API. Host `db` chỉ dùng giữa các container; API chạy trên máy dùng `localhost` và cổng MySQL được mở trên máy.

### Frontend

Mở terminal khác:

```sh
cd frontend
npm ci
npm run dev
```

Mở **http://localhost:5173**. Vite đã cấu hình chuyển tiếp `/api` tới `http://localhost:5039`. Frontend mặc định gọi `/api/v1`; nếu thay `VITE_API_BASE_URL` trong môi trường frontend, cần giữ đường dẫn phù hợp với API và cơ chế chuyển tiếp.

## Thiết lập dữ liệu lần đầu

Khi API khởi động, ứng dụng tạo schema nếu database chưa tồn tại, thực hiện các bước bổ sung schema cũ, rồi tạo danh mục lĩnh vực, trình độ mặc định và tài khoản **`admin@languagelearning.app`**. Mật khẩu quản trị lấy từ `BOOTSTRAP_ADMIN_PASSWORD` khi dùng Compose, hoặc `Bootstrap__AdminPassword` khi chạy trực tiếp. Đổi biến này không đặt lại mật khẩu của tài khoản đã có.

Database mới chưa có ngôn ngữ, người học, khóa học hay bộ thẻ mẫu. Để bắt đầu:

1. Đăng nhập bằng tài khoản quản trị, vào `/admin` và tạo ngôn ngữ.
2. Tạo khóa học với ngôn ngữ, trình độ và lĩnh vực phù hợp.
3. Tải mẫu JSON của chính khóa học đó từ trang quản trị, điền nội dung rồi nhập lại; hoặc biên soạn qua các màn hình quản lý nội dung.
4. Kiểm tra nội dung và trạng thái xuất bản của khóa học. Xem [hướng dẫn JSON giáo trình](docs/templates/README.md) để khai báo từ vựng, kỹ năng và các bước hoàn thành bài.
5. Đăng ký tài khoản người học, chọn mục tiêu khớp với khóa học đã tạo rồi bắt đầu học.

API hiện khởi tạo bằng `EnsureCreated` kết hợp các bước nâng cấp schema trong `LegacySchemaUpgrade`, chưa áp dụng toàn bộ EF migration bằng `Migrate`. Không chạy `dotnet ef database update` trực tiếp trên database đã tạo bằng `EnsureCreated` khi chưa chuẩn bị baseline lịch sử migration.

Có thể kiểm tra việc nâng cấp schema trên bản sao tạm của MySQL local bằng [scripts/verify-local-mysql.ps1](scripts/verify-local-mysql.ps1). Đọc cấu hình và tham số của script trước khi chạy; script sao lưu, phục hồi sang database tạm, khởi động API để kiểm tra rồi xóa bản sao.

## Kiểm tra mã nguồn

Từ thư mục gốc, build và chạy unit test cùng integration test backend:

```sh
dotnet build backend/LanguageLearning.sln --configuration Release
dotnet test backend/LanguageLearning.sln --configuration Release
```

Các integration test dùng `CustomWebApplicationFactory` tạo database SQLite in-memory và dữ liệu kiểm thử riêng.

Trong thư mục `frontend`:

```sh
npm ci
npm run lint
npm run build
npx playwright install chromium
npm run test:e2e
```

Playwright tự khởi động Vite tại cổng `5173`, hoặc dùng lại server đang chạy. Các bài kiểm thử gọi API thật cần backend chạy riêng tại cổng `5039` và có dữ liệu phù hợp. Các bài đăng nhập bằng tài khoản mẫu cần tạo tài khoản đó trước; mật khẩu người học có thể truyền qua `PLAYWRIGHT_LEARNER_PASSWORD`. Bộ kiểm thử hành trình bài học chỉ bật khi `PLAYWRIGHT_GUIDED_E2E=1` và cần database kiểm thử riêng. Có thể dùng `npm run test:e2e:ui` để mở giao diện kiểm thử.

GitHub Actions có workflow build/test backend và build frontend. Lệnh build frontend bao gồm kiểm tra TypeScript; workflow frontend hiện chưa chạy Playwright.

## Cấu trúc repository

```text
backend/
  src/
    LanguageLearning.Api/             Controllers, middleware, cấu hình khởi động
    LanguageLearning.Application/     Dịch vụ nghiệp vụ, DTO, interface
    LanguageLearning.Domain/          Thực thể, enum
    LanguageLearning.Infrastructure/  EF Core, repository, bảo mật, lưu tệp
  tests/
    LanguageLearning.Tests/           Unit test
    LanguageLearning.IntegrationTests/
frontend/
  src/
    features/                        Các màn hình và chức năng
    components/                      Thành phần giao diện dùng chung
    services/                        API client, nội dung học, giọng nói
    offline/                         IndexedDB và dữ liệu đồng bộ
    i18n/                            Bản dịch giao diện Việt/Anh
  e2e/                               Kiểm thử Playwright
docs/                                Tài liệu thiết kế, API, dữ liệu, kiểm thử
scripts/                             Công cụ hỗ trợ dữ liệu và phát triển
.github/workflows/                   CI backend và frontend
docker-compose.yml
.env.example
```

## Tài liệu và đóng góp

- [Tài liệu tổng quan dự án](LANGUAGE_LEARNING_PROJECT_DOCUMENTATION.md)
- [Cấu trúc cơ sở dữ liệu](docs/database/database-schema.md)
- [Danh sách API](docs/api/api-endpoints-reference.md) và [quy ước API](docs/api/api-conventions.md)
- [Hướng dẫn nội dung JSON](docs/templates/README.md)
- [Hướng dẫn kiểm thử](docs/testing/automation-testing-guide.md)
- [Đánh giá mức độ sẵn sàng triển khai](docs/production-readiness.md)

Khi đóng góp, đọc [CONTRIBUTING.md](CONTRIBUTING.md) và [quy chuẩn mã nguồn](docs/standards/coding-and-ai-guidelines.md). Tạo nhánh từ `develop`, dùng Conventional Commits và mở pull request vào `develop` kèm mô tả thay đổi, kết quả kiểm tra. Quy tắc ứng xử nằm trong [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md); cách báo lỗi bảo mật nằm trong [SECURITY.md](SECURITY.md).

Dự án sử dụng [giấy phép MIT](LICENSE).
