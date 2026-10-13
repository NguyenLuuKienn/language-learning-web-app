# ADR-001: Lựa chọn Kiến trúc Clean Architecture và Định danh Thực thể bằng GUID

- **Trạng thái:** Đã chấp thuận (Accepted)
- **Ngày quyết định:** 2026-10-13
- **Người quyết định:** Leader & Maintainer

## Bối cảnh (Context)
Dự án Language Learning Web Application đòi hỏi:
1. Hỗ trợ học tập ngoại tuyến (Offline-First) và đồng bộ dữ liệu (Data Sync) khi kết nối mạng trở lại.
2. Nhiều thành viên phân chia lập trình theo các tầng riêng biệt.
3. Chống xung đột khóa chính khi tạo mới thực thể (Cards, Decks, Review Events) phía Client khi đang ngắt kết nối Internet.

## Quyết định (Decision)
1. **Kiến trúc Backend:** Áp dụng **Clean Architecture** phân tách thành 4 tầng rõ ràng:
   - `Domain`: Chứa Entity, Enums, Value Objects, Domain Events, không phụ thuộc tầng nào.
   - `Application`: Chứa Use Cases, DTOs, Interface Services, Fluent Validation.
   - `Infrastructure`: Chứa EF Core DbContext, MySQL Configurations, Authentication Services, External Integrations.
   - `Api`: Chứa Controllers, Swagger UI, Middleware, cấu hình Dependency Injection.
2. **Khóa chính Entity (Primary Keys):**
   - Sử dụng kiểu dữ liệu `Guid` trong C# (`Guid.NewGuid()`).
   - Lưu trữ trong MySQL dưới dạng chuỗi `CHAR(36)` để dễ debug, truy vấn và tương thích đồng bộ JSON API.
   - Phía Client sử dụng UUID v4 cho các thực thể tạo offline, cho phép server ghi nhận trực tiếp mà không bị xung đột Auto-Increment ID.

## Hệ quả (Consequences)
- **Tích cực:** Client có thể sinh ID ngay tại IndexedDB cục bộ mà không cần đợi Server cấp ID; tránh lộ số thứ tự tăng dần; phân tách rõ ràng trách nhiệm giữa các lớp code.
- **Tiêu cực:** Dung lượng lưu trữ `CHAR(36)` lớn hơn một chút so với Integer hay `BINARY(16)`, cần đánh index hợp lý cho các khóa ngoại (`user_id`, `deck_id`, `course_id`).
