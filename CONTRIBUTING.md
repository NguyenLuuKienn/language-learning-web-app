# Quy định và Hướng dẫn Đóng góp (CONTRIBUTING GUIDELINES)

Chào mừng bạn đến với dự án **Language Learning Web Application**!  
Để đảm bảo chất lượng mã nguồn, tính nhất quán của kiến trúc Clean Architecture và tránh lỗi khi các thành viên sử dụng công cụ AI sinh mã (AI-assisted coding), toàn bộ thành viên trong nhóm bắt buộc phải tuân thủ nghiêm ngặt các quy tắc dưới đây trước khi commit và mở Pull Request.

---

## 🚨 1. QUY TẮC BẮT BUỘC VỀ VIẾT CODE & SỬ DỤNG AI

### 1.1. Quy chuẩn Comment trong Code:
* ❌ **CẤM TUYỆT ĐỐI đưa icon/emoji vào comment code:** Không sử dụng các emoji như 🚀, 💡, 🔥, ✨, ⚠️ trong file mã nguồn (`.cs`, `.ts`, `.tsx`, `.js`).
* ❌ **HẠN CHẾ comment dư thừa:** Không comment lại những gì code đã thể hiện rõ ràng (ví dụ: cấm viết `// hàm lấy user` ngay trên hàm `GetUser()`). Chỉ comment khi thực sự cần thiết để giải thích lý do nghiệp vụ đặc thù (*Explain WHY, not WHAT*).
* 🌐 **BẮT BUỘC comment bằng Tiếng Anh (English Only):** Toàn bộ comment kỹ thuật, XML docstrings, JSDoc trong mã nguồn phải viết bằng tiếng Anh chuẩn.

### 1.2. Bảo vệ Cấu trúc Dự án (Architectural Integrity):
* 🏗️ **Backend Clean Architecture:**
  * Tầng `Domain` là lõi độc lập: Tuyệt đối không được reference hay import thư viện của tầng ngoài (`Infrastructure`, `Application`, `Api`).
  * `Controllers` trong `Api` chỉ điều phối request/response, **tuyệt đối không viết business logic trực tiếp trong Controller**. Mọi xử lý nghiệp vụ phải nằm trong Service/Application layer.
  * Khóa chính thực thể bắt buộc dùng `Guid` (định dạng chuỗi `CHAR(36)` thống nhất).
* ⚛️ **Frontend Feature-First Architecture:**
  * Mã nguồn chia theo module nghiệp vụ tại `src/features/<module-name>`.
  * TypeScript Strict Mode: **CẤM lạm dụng kiểu `any`**. Mọi dữ liệu phải có interface/type rõ ràng trong `types/`.
  * Tách biệt logic API và giao diện UI, sử dụng `apiClient.ts` tập trung.

### 1.3. Kỷ luật Kiểm thử & Build Trước khi Push:
* 🧪 **Luôn chạy test và build cục bộ thành công 100% trước khi push:**
  * **Backend:** Chạy `dotnet test backend/LanguageLearning.sln` và `dotnet build backend/LanguageLearning.sln` (đạt **0 Warning, 0 Error**).
  * **Frontend:** Chạy `npm run build` trong thư mục `frontend/` (đạt **Build passed**).
  * ❌ **Nghiêm cấm push code khi chưa chạy thử ở local hoặc cố tình bỏ qua test bị fail.**

### 1.4. An toàn Bảo mật & Dữ liệu Nhạy cảm:
* 🔒 **Tuyệt đối không commit secrets:** Không commit `.env`, mật khẩu cơ sở dữ liệu thật, JWT secret key, API keys cá nhân.
* 🧹 **Không commit tệp rác:** Đảm bảo `.gitignore` đã chặn các thư mục `bin/`, `obj/`, `node_modules/`, `dist/`, `.vs/`, `.idea/`.

---

## 🌿 2. QUY TRÌNH PHÁT TRIỂN: ISSUE → BRANCH → COMMIT → PR

### Bước 1: Nhận Issue & Tạo Nhánh Tính Năng
* Mọi công việc phải xuất phát từ một Issue trên GitHub Projects.
* Nhánh phải được tách từ nhánh **`develop`**:
  * Tính năng mới: `feature/<issue-number>-<short-description>` (Ví dụ: `feature/10-auth-register-login`)
  * Sửa lỗi: `fix/<issue-number>-<short-description>`
  * Kiểm thử: `test/<issue-number>-<short-description>`
* ❌ **Cấm tự ý tách nhánh từ `main` hoặc push trực tiếp vào `main`/`develop`.**

### Bước 2: Commit theo Chuẩn Conventional Commits
Tiêu đề commit phải rõ ràng, tuân thủ tiền tố:
* `feat:` Tính năng mới (ví dụ: `feat(auth): add register and login endpoints (Closes #10)`)
* `fix:` Sửa lỗi
* `refactor:` Tái cấu trúc code (không đổi tính năng)
* `test:` Thêm hoặc sửa test cases
* `style:` Định dạng code, chuẩn hóa lint
* `docs:` Cập nhật tài liệu

### Bước 3: Mở Pull Request vào nhánh `develop`
* Target branch bắt buộc phải là **`develop`**.
* Điền đầy đủ thông tin theo mẫu **Pull Request Template**.
* Gắn liên kết đóng Issue: `Closes #<issue-number>`.
* Đính kèm ảnh chụp màn hình UI hoặc kết quả test thành công.

### Bước 4: Code Review & Phê duyệt
* PR phải có **ít nhất 1 Approval** từ thành viên khác hoặc Leader/Maintainer.
* Toàn bộ các luồng kiểm tra tự động của CI (GitHub Actions) phải đạt màu xanh.
* Không được tự ý bypass rules trừ trường hợp đặc biệt được Leader chỉ định.

---

## ✅ 3. ĐỊNH NGHĨA HOÀN THÀNH (DEFINITION OF DONE - DoD)

Một task/issue chỉ được chấp nhận là `Done` khi:
1. Code đã được merge vào `develop` thông qua Pull Request hợp lệ.
2. Tất cả bài kiểm tra tự động của GitHub Actions CI đều Passed.
3. Không vi phạm bất kỳ quy tắc code / AI nào ở Mục 1.
4. Đạt đủ 100% Tiêu chí nghiệm thu (Acceptance Criteria) được mô tả trong Issue.
