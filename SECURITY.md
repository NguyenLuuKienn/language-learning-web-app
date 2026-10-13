# Chính sách Bảo mật (SECURITY POLICY)

## 1. Phạm vi phiên bản hỗ trợ

| Phiên bản | Hỗ trợ bảo mật |
|---|---|
| `v1.0.x` | :white_check_mark: Được hỗ trợ |
| `< v1.0.0` | :x: Phiên bản tiền phát hành (Development) |

## 2. Báo cáo lỗ hổng bảo mật

Nếu bạn phát hiện bất kỳ vấn đề bảo mật nào liên quan đến dự án (như lộ mật khẩu, JWT token không an toàn, Injection, v.v.):
1. **Vui lòng KHÔNG mở Issue công khai.**
2. Hãy gửi email trực tiếp cho Leader hoặc Maintainer của dự án.
3. Cung cấp chi tiết:
   - Các bước tái hiện (Proof of Concept).
   - Mức độ ảnh hưởng ước tính.
   - Gợi ý cách khắc phục (nếu có).

## 3. Các quy định bảo mật bắt buộc trong dự án

- **Không bao giờ commit secrets:** Tuyệt đối không commit file `.env`, chuỗi kết nối database thật, JWT secret key hoặc thông tin đăng nhập lên Git repository.
- **Mã hóa mật khẩu:** Mật khẩu người dùng bắt buộc phải được băm (hash) bằng PBKDF2/BCrypt hoặc ASP.NET Core Identity PasswordHasher.
- **Xác thực quyền hạn (Authorization):** Mọi API nhạy cảm phải kiểm tra quyền của người dùng ở phía Server, không tin cậy hoàn toàn vào trạng thái UI.
- **An toàn tệp tin:** Kiểm tra MIME-type, định dạng và dung lượng tối đa đối với các tệp tin hình ảnh/âm thanh tải lên.
- **Bảo mật dữ liệu ngoại tuyến (Offline):** Không lưu mật khẩu hoặc Refresh Token vào IndexedDB phía trình duyệt. Dữ liệu offline phải được dọn sạch khi người dùng đăng xuất.
