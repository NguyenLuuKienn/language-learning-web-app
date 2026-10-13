## 📌 Tóm tắt Thay đổi (Summary)
<!-- Mô tả ngắn gọn mục đích của PR này. Đã làm gì và giải quyết vấn đề gì? -->

## 🔗 Liên kết Issue (Linked Issue)
<!-- Gắn issue liên quan để tự động đóng khi merge: Closes #10 hoặc Fixes #10 -->
Closes #

## 🛠️ Danh sách việc đã làm (Changes Details)
- [ ] 
- [ ] 

## 🧪 Bằng chứng Kiểm thử Cục bộ (Local Test Evidence)
<!-- BẮT BUỘC: Điền kết quả test hoặc đính kèm ảnh chụp màn hình UI -->
- **Backend Build & Test:** `dotnet test` & `dotnet build` (0 Warning, 0 Error)
- **Frontend Build:** `npm run build` (Passed)
- **Ảnh chụp UI / Swagger Response:** 

## 🛡️ Checklist Tuân thủ Quy tắc Dự án & AI (BẮT BUỘC TICK ĐẦY ĐỦ)
- [ ] **Comment Rules:** Tuyệt đối KHÔNG có icon/emoji trong comment code; comment hoàn toàn bằng Tiếng Anh; không comment dư thừa.
- [ ] **Architecture:** Không phá hỏng cấu trúc Clean Architecture (Domain không phụ thuộc tầng ngoài, Controller không chứa business logic).
- [ ] **TypeScript:** Không lạm dụng kiểu `any`; định nghĩa types/interfaces đầy đủ.
- [ ] **Pre-push Test:** Đã chạy test và build thành công 100% ở local trước khi push.
- [ ] **Security & Clean:** Không commit secrets, password thật, file `.env` hay tệp rác (`bin/`, `obj/`, `node_modules/`).
- [ ] **Branch Target:** PR này đang trỏ vào nhánh `develop` (KHÔNG trỏ thẳng vào `main`).
