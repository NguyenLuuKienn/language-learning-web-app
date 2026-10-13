# Quy định và Hướng dẫn Đóng góp (CONTRIBUTING)

Chào mừng bạn đến với dự án **Language Learning Web Application**! Đây là dự án phục vụ môn học *Phát triển phần mềm mã nguồn mở*, hoạt động theo chuẩn mực đóng góp của cộng đồng OSS quốc tế.

---

## 1. Nguyên tắc làm việc & Vai trò

- **Leader:** Quản trị Roadmap, phân rã Issue, Project Board, quản lý Milestone, nghiệm thu Acceptance Criteria.
- **Maintainer:** Quản trị nhánh chính, thiết lập CI/CD, chuẩn hóa Coding Convention, Review Pull Request, Release.
- **Core Developer & Contributor:** Nhận Issue, tạo branch tính năng, lập trình, viết Unit/Integration Tests, mở PR và giải trình.

---

## 2. Quy trình Phát triển: Issue → Branch → Commit → PR

### Bước 1: Chọn và Nhận Issue
- Mọi công việc đều bắt đầu từ một Issue trên GitHub Projects.
- Đảm bảo Issue có đầy đủ: Mô tả công việc, Acceptance Criteria, Assignee, Labels, Milestone.
- Chuyển trạng thái Issue sang `In Progress` trên GitHub Projects Board.

### Bước 2: Tạo Nhánh (Branching Strategy)
- Nhánh `main`: Ổn định, chỉ dùng cho các bản Release đã kiểm thử (Protected).
- Nhánh `develop`: Nhánh tích hợp chính. Mọi feature PR đều phải trỏ vào `develop` (Protected).
- Đặt tên nhánh theo cấu trúc:
  - Tính năng mới: `feature/<issue-number>-<short-description>` (Ví dụ: `feature/10-auth-register-login`)
  - Sửa lỗi: `fix/<issue-number>-<short-description>` (Ví dụ: `fix/27-srs-idempotency-bug`)
  - Viết tài liệu: `docs/<issue-number>-<short-description>`
  - Kiểm thử: `test/<issue-number>-<short-description>`

### Bước 3: Định dạng Commit (Conventional Commits)
Thông điệp commit phải viết bằng tiếng Anh hoặc tiếng Việt rõ ràng, tuân thủ tiền tố:
- `feat:` Thêm chức năng mới (Feature)
- `fix:` Sửa lỗi (Bug fix)
- `docs:` Thay đổi hoặc bổ sung tài liệu
- `style:` Định dạng code, dấu cách, chấm phẩy (không đổi logic code)
- `refactor:` Tái cấu trúc code mà không sửa bug hay thêm feature
- `test:` Bổ sung hoặc chỉnh sửa test cases
- `chore:` Thay đổi build tool, dependencies, cấu hình CI

*Ví dụ:* `feat(auth): implement jwt token generation and refresh token flow (#11)`

### Bước 4: Mở Pull Request (PR)
- Tạo PR từ nhánh của bạn vào nhánh `develop`.
- Sử dụng mẫu **Pull Request Template** được cung cấp.
- Ghi rõ từ khóa đóng Issue: `Closes #<issue-number>` hoặc `Fixes #<issue-number>`.
- Đính kèm hình ảnh giao diện / bằng chứng kiểm thử (Unit test passing, API response).

### Bước 5: Code Review & Hợp nhất (Merge)
- Ít nhất 1 thành viên khác (hoặc Maintainer/Leader) phải review và Approve.
- Tất cả các bước kiểm tra tự động của CI (GitHub Actions) phải đạt màu xanh (Pass).
- Sau khi được phê duyệt, sử dụng hình thức **Squash and Merge** hoặc **Rebase and Merge** vào `develop`.

---

## 3. Tiêu chuẩn Mã nguồn (Coding Standards)

- **Backend (C# / .NET):**
  - Tuân thủ Clean Architecture: Controller gọi Service/Handler, không viết logic trực tiếp trong Controller.
  - Sử dụng Dependency Injection (DI) đầy đủ.
  - Sử dụng GUID cho tất cả các định danh Entity.
  - Xử lý lỗi trả về chuẩn Problem Details (`application/problem+json`).
- **Frontend (TypeScript / React):**
  - Sử dụng TypeScript strict mode, tránh dùng kiểu `any`.
  - Phân chia component theo feature (`src/features/...`).
  - Sử dụng TanStack Query cho việc fetch và cache dữ liệu API.
  - Đảm bảo giao diện responsive từ mobile (360px) đến desktop (1280px).

---

## 4. Định nghĩa Hoàn thành (Definition of Done - DoD)

Một Issue chỉ được tính là hoàn thành (`Done`) khi:
1. Mã nguồn đã được merge vào nhánh `develop` qua PR.
2. Tất cả bài kiểm tra CI chạy thành công (Lint, Build, Unit/Integration tests).
3. Đã viết Unit Test hoặc Integration Test cho logic nghiệp vụ mới.
4. Đã cập nhật tài liệu liên quan (OpenAPI spec hoặc docs).
5. Đáp ứng toàn bộ Acceptance Criteria được ghi trong Issue ban đầu.
