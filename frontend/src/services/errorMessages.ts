const messages = {
  en: {
    AUTH_EMAIL_IN_USE: 'This email address is already in use.',
    AUTH_INVALID_CREDENTIALS: 'Invalid email or password.',
    AUTH_ACCOUNT_DEACTIVATED: 'This account has been deactivated.',
    AUTH_INVALID_REFRESH_TOKEN: 'Your session has expired. Please sign in again.',
    AUTH_INCORRECT_PASSWORD: 'The current password is incorrect.',
    COURSE_TITLE_INVALID: 'Course title must contain 2 to 200 visible characters.',
    COURSE_NOT_FOUND: 'The requested course was not found.',
    LESSON_CHECKPOINT_CONFLICT: 'This lesson changed in another tab or device. Load the latest progress before saving again.',
    LESSON_CONTENT_CHANGED: 'The lesson content changed. Reload the lesson before continuing.',
    LESSON_STEP_IDS_IN_USE: 'Learners have started this lesson. Keep its step IDs and types unchanged.',
    LESSON_ACTIVITY_IN_USE: 'Learners have started this lesson. Create a new lesson to change assessed activities.',
    LESSON_STEP_POLICY_IN_USE: 'Learners have started this lesson. Keep step completion rules unchanged.',
    NOTEBOOK_EMPTY: 'Write a word or a note before saving.',
    NOTEBOOK_TOO_LONG: 'This note is too long.',
    NOTEBOOK_SEARCH_TOO_LONG: 'Search may contain at most 200 characters.'
  },
  vi: {
    AUTH_EMAIL_IN_USE: 'Email này đã được sử dụng.',
    AUTH_INVALID_CREDENTIALS: 'Email hoặc mật khẩu không đúng.',
    AUTH_ACCOUNT_DEACTIVATED: 'Tài khoản đã bị vô hiệu hóa.',
    AUTH_INVALID_REFRESH_TOKEN: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
    AUTH_INCORRECT_PASSWORD: 'Mật khẩu hiện tại không đúng.',
    COURSE_TITLE_INVALID: 'Tên khóa học phải có từ 2 đến 200 ký tự hiển thị.',
    COURSE_NOT_FOUND: 'Không tìm thấy khóa học.',
    LESSON_CHECKPOINT_CONFLICT: 'Bài học đã thay đổi ở tab hoặc thiết bị khác. Hãy tải tiến độ mới trước khi lưu tiếp.',
    LESSON_CONTENT_CHANGED: 'Nội dung bài học đã thay đổi. Hãy tải lại bài trước khi tiếp tục.',
    LESSON_STEP_IDS_IN_USE: 'Đã có người bắt đầu bài này. Hãy giữ nguyên ID và loại bước học.',
    LESSON_ACTIVITY_IN_USE: 'Đã có người bắt đầu bài này. Hãy tạo bài mới nếu cần thay nội dung luyện tập.',
    LESSON_STEP_POLICY_IN_USE: 'Đã có người bắt đầu bài này. Hãy giữ nguyên điều kiện hoàn thành bước.',
    NOTEBOOK_EMPTY: 'Nhập một từ hoặc ghi chú trước khi lưu.',
    NOTEBOOK_TOO_LONG: 'Ghi chú vượt quá độ dài cho phép.',
    NOTEBOOK_SEARCH_TOO_LONG: 'Từ khóa tìm kiếm tối đa 200 ký tự.'
  }
} as const;

export function localizeApiError(code: string): string | undefined {
  const language = localStorage.getItem('app_language') === 'vi' ? 'vi' : 'en';
  return (messages[language] as Record<string, string>)[code];
}
