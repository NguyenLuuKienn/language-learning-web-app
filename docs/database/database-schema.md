# Thiết kế Cơ sở Dữ liệu (Database Schema)

Dự án sử dụng cơ sở dữ liệu **MySQL 8.0** kết hợp với **Entity Framework Core (Pomelo Provider)**.

## Quy ước Đặt tên & Kiểu Dữ liệu
- Tên bảng: `snake_case` số nhiều (Ví dụ: `users`, `courses`, `cards`).
- Tên cột: `snake_case` (Ví dụ: `created_at`, `user_id`, `display_name`).
- Khóa chính (`id`) và Khóa ngoại (`*_id`): Định dạng `CHAR(36)` (UUID v4 / GUID).
- Thời gian: `created_at`, `updated_at` sử dụng kiểu UTC `DATETIME(6)`.
- Khóa kiểm soát xung đột: `row_version` (hoặc `version` INT).

## Danh sách 24 Bảng Cốt lõi

| Bảng | Chức năng | Các trường chính |
|---|---|---|
| `users` | Người dùng hệ thống | `id`, `email`, `password_hash`, `display_name`, `role`, `timezone`, `status` |
| `refresh_tokens` | Quản lý phiên làm việc JWT | `id`, `user_id`, `token_hash`, `expires_at`, `revoked_at` |
| `languages` | Danh mục ngôn ngữ học | `id`, `code`, `name`, `is_active` |
| `courses` | Khóa học theo ngôn ngữ/trình độ | `id`, `language_id`, `title`, `level`, `description`, `status` |
| `units` | Chương học trong khóa | `id`, `course_id`, `title`, `sort_order` |
| `lessons` | Bài học trong chương | `id`, `unit_id`, `title`, `content`, `sort_order`, `status` |
| `vocabularies` | Từ vựng của bài học | `id`, `lesson_id`, `term`, `phonetic`, `meaning`, `example`, `language_id` |
| `decks` | Bộ thẻ học từ vựng | `id`, `owner_id`, `language_id`, `title`, `description`, `visibility`, `source_deck_id`, `version` |
| `deck_tags` | Nhãn phân loại bộ thẻ | `id`, `deck_id`, `tag` |
| `cards` | Thẻ Flashcard hai mặt | `id`, `deck_id`, `front`, `back`, `version` |
| `media_assets` | Quản lý file ảnh/audio | `id`, `owner_id`, `storage_key`, `mime_type`, `size_bytes`, `kind` |
| `card_media` | Liên kết thẻ và media | `id`, `card_id`, `media_id`, `side` |
| `vocabulary_media`| Liên kết từ vựng và media | `id`, `vocabulary_id`, `media_id` |
| `card_schedules` | Lịch ôn tập Spaced Repetition | `id`, `user_id`, `card_id`, `due_at`, `state_json`, `algorithm_version`, `last_reviewed_at` |
| `review_events` | Nhật ký sự kiện chấm điểm SRS | `id`, `user_id`, `card_id`, `rating`, `reviewed_at`, `client_event_id`, `algorithm_version` |
| `questions` | Ngân hàng câu hỏi bài tập | `id`, `lesson_id`, `type`, `prompt`, `explanation`, `config_json` |
| `question_options`| Lựa chọn câu hỏi trắc nghiệm | `id`, `question_id`, `content`, `is_correct`, `sort_order` |
| `quiz_attempts` | Lịch sử làm bài quiz | `id`, `user_id`, `lesson_id`, `score`, `max_score`, `completed_at` |
| `quiz_answers` | Chi tiết câu trả lời bài quiz | `id`, `attempt_id`, `question_id`, `answer_json`, `is_correct` |
| `lesson_progress` | Tiến độ học tập từng bài | `id`, `user_id`, `lesson_id`, `status`, `completed_at`, `last_accessed_at` |
| `course_enrollments`| Đăng ký tham gia khóa học | `id`, `user_id`, `course_id`, `enrolled_at` |
| `daily_learning_stats`| Thống kê học tập mỗi ngày | `id`, `user_id`, `local_date`, `timezone`, `review_count`, `lessons_completed`, `goal_met` |
| `user_goals` | Mục tiêu số thẻ cần học | `id`, `user_id`, `daily_target`, `goal_type` |
| `sync_operations` | Nhật ký đồng bộ & Idempotency | `id`, `user_id`, `client_operation_id`, `operation_type`, `entity_id`, `payload_hash`, `status` |
| `audit_logs` | Nhật ký hành vi quản trị viên | `id`, `actor_id`, `action`, `target_type`, `target_id`, `metadata_json` |
