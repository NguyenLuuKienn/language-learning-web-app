export interface AuthFormValues {
  email: string;
  password: string;
  displayName: string;
  confirmPassword: string;
}

export type AuthFormErrors = Partial<Record<keyof AuthFormValues, string>>;

export function validateAuthForm(
  values: AuthFormValues,
  mode: 'login' | 'register'
): AuthFormErrors {
  const errors: AuthFormErrors = {};
  const email = values.email.trim();

  if (!email) {
    errors.email = 'Vui lòng nhập email.';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    errors.email = 'Email không hợp lệ.';
  }

  if (!values.password) {
    errors.password = 'Vui lòng nhập mật khẩu.';
  } else if (values.password.length < 8) {
    errors.password = 'Mật khẩu cần ít nhất 8 ký tự.';
  }

  if (mode === 'register') {
    if (values.displayName.trim().length > 80) {
      errors.displayName = 'Tên hiển thị không được quá 80 ký tự.';
    }

    if (!values.confirmPassword) {
      errors.confirmPassword = 'Vui lòng xác nhận mật khẩu.';
    } else if (values.confirmPassword !== values.password) {
      errors.confirmPassword = 'Mật khẩu xác nhận không khớp.';
    }
  }

  return errors;
}
