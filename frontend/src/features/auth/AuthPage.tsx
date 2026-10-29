import { useState } from 'react';
import { ArrowRight, BookOpen, Eye, EyeOff, LockKeyhole, Mail, UserRound } from 'lucide-react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';

import { useAuth } from './authContext';
import { validateAuthForm } from './authValidation';

import type { FormEvent } from 'react';
import type { AuthFormErrors, AuthFormValues } from './authValidation';

const INITIAL_VALUES: AuthFormValues = {
  email: '',
  password: '',
  displayName: '',
  confirmPassword: '',
};

function getReturnPath(state: unknown): string {
  if (
    typeof state === 'object' &&
    state !== null &&
    'from' in state &&
    typeof state.from === 'string' &&
    state.from.startsWith('/') &&
    !state.from.startsWith('//') &&
    !state.from.startsWith('/login') &&
    !state.from.startsWith('/register')
  ) {
    return state.from;
  }

  return '/';
}

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { user, isRestoring, login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [values, setValues] = useState<AuthFormValues>(INITIAL_VALUES);
  const [errors, setErrors] = useState<AuthFormErrors>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const isRegister = mode === 'register';

  if (isRestoring) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#FAF6EE] text-stone-600">
        Đang khôi phục phiên đăng nhập...
      </div>
    );
  }

  if (user) {
    return <Navigate to={getReturnPath(location.state)} replace />;
  }

  function updateValue(field: keyof AuthFormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
    setServerError(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateAuthForm(values, mode);
    setErrors(validationErrors);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSubmitting(true);
    setServerError(null);

    try {
      const email = values.email.trim().toLowerCase();

      if (isRegister) {
        await register({
          email,
          password: values.password,
          displayName: values.displayName.trim(),
        });
      } else {
        await login({ email, password: values.password });
      }

      navigate(getReturnPath(location.state), { replace: true });
    } catch (error) {
      setServerError(
        error instanceof Error
          ? error.message
          : 'Không thể kết nối. Vui lòng thử lại.'
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="graph-paper flex min-h-screen items-center justify-center px-4 py-10 text-stone-900 sm:px-6">
      <div className="w-full max-w-md">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl border-2 border-stone-900 bg-rose-500 text-white shadow-[3px_3px_0_#262626]">
            <BookOpen size={28} aria-hidden="true" />
          </div>
          <p className="text-xs font-extrabold uppercase tracking-[0.25em] text-rose-600">
            KOTOBA
          </p>
          <h1 className="mt-2 text-3xl font-extrabold tracking-tight">
            {isRegister ? 'Tạo tài khoản' : 'Chào mừng trở lại'}
          </h1>
          <p className="mt-2 text-sm text-stone-600">
            {isRegister
              ? 'Bắt đầu hành trình học tập của riêng bạn.'
              : 'Đăng nhập để tiếp tục học và quản lý bộ thẻ.'}
          </p>
        </div>

        <form
          noValidate
          onSubmit={(event) => void handleSubmit(event)}
          className="space-y-4 rounded-2xl border-2 border-stone-900 bg-white p-6 shadow-[5px_5px_0_#262626] sm:p-8"
        >
          {serverError && (
            <p role="alert" className="rounded-lg border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-700">
              {serverError}
            </p>
          )}

          {isRegister && (
            <div>
              <label htmlFor="auth-display-name" className="mb-1.5 block text-sm font-bold">
                Tên hiển thị <span className="font-normal text-stone-400">(không bắt buộc)</span>
              </label>
              <div className="relative">
                <UserRound size={18} aria-hidden="true" className="absolute left-3 top-3 text-stone-400" />
                <input
                  id="auth-display-name"
                  type="text"
                  autoComplete="name"
                  value={values.displayName}
                  onChange={(event) => updateValue('displayName', event.target.value)}
                  aria-invalid={Boolean(errors.displayName)}
                  aria-describedby={errors.displayName ? 'display-name-error' : undefined}
                  className="w-full rounded-lg border-2 border-stone-300 py-2.5 pl-10 pr-3 text-sm outline-none focus:border-rose-500"
                  placeholder="Tên của bạn"
                />
              </div>
              {errors.displayName && <p id="display-name-error" className="mt-1 text-xs text-red-600">{errors.displayName}</p>}
            </div>
          )}

          <div>
            <label htmlFor="auth-email" className="mb-1.5 block text-sm font-bold">Email</label>
            <div className="relative">
              <Mail size={18} aria-hidden="true" className="absolute left-3 top-3 text-stone-400" />
              <input
                id="auth-email"
                type="email"
                autoComplete="email"
                value={values.email}
                onChange={(event) => updateValue('email', event.target.value)}
                aria-invalid={Boolean(errors.email)}
                aria-describedby={errors.email ? 'email-error' : undefined}
                className="w-full rounded-lg border-2 border-stone-300 py-2.5 pl-10 pr-3 text-sm outline-none focus:border-rose-500"
                placeholder="ban@example.com"
              />
            </div>
            {errors.email && <p id="email-error" className="mt-1 text-xs text-red-600">{errors.email}</p>}
          </div>

          <div>
            <label htmlFor="auth-password" className="mb-1.5 block text-sm font-bold">Mật khẩu</label>
            <div className="relative">
              <LockKeyhole size={18} aria-hidden="true" className="absolute left-3 top-3 text-stone-400" />
              <input
                id="auth-password"
                type={showPassword ? 'text' : 'password'}
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                value={values.password}
                onChange={(event) => updateValue('password', event.target.value)}
                aria-invalid={Boolean(errors.password)}
                aria-describedby={errors.password ? 'password-error' : undefined}
                className="w-full rounded-lg border-2 border-stone-300 py-2.5 pl-10 pr-11 text-sm outline-none focus:border-rose-500"
                placeholder="Ít nhất 8 ký tự"
              />
              <button
                type="button"
                onClick={() => setShowPassword((current) => !current)}
                aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                className="absolute right-3 top-2.5 text-stone-500 hover:text-stone-800"
              >
                {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
              </button>
            </div>
            {errors.password && <p id="password-error" className="mt-1 text-xs text-red-600">{errors.password}</p>}
          </div>

          {isRegister && (
            <div>
              <label htmlFor="auth-confirm-password" className="mb-1.5 block text-sm font-bold">Xác nhận mật khẩu</label>
              <input
                id="auth-confirm-password"
                type="password"
                autoComplete="new-password"
                value={values.confirmPassword}
                onChange={(event) => updateValue('confirmPassword', event.target.value)}
                aria-invalid={Boolean(errors.confirmPassword)}
                aria-describedby={errors.confirmPassword ? 'confirm-password-error' : undefined}
                className="w-full rounded-lg border-2 border-stone-300 px-3 py-2.5 text-sm outline-none focus:border-rose-500"
                placeholder="Nhập lại mật khẩu"
              />
              {errors.confirmPassword && <p id="confirm-password-error" className="mt-1 text-xs text-red-600">{errors.confirmPassword}</p>}
            </div>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="flex w-full items-center justify-center gap-2 rounded-lg border-2 border-stone-900 bg-rose-500 px-4 py-3 text-sm font-extrabold text-white transition hover:bg-rose-600 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting
              ? 'Đang xử lý...'
              : isRegister ? 'Tạo tài khoản' : 'Đăng nhập'}
            {!isSubmitting && <ArrowRight size={18} aria-hidden="true" />}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-stone-600">
          {isRegister ? 'Đã có tài khoản?' : 'Chưa có tài khoản?'}{' '}
          <Link
            to={isRegister ? '/login' : '/register'}
            state={location.state}
            className="font-bold text-rose-700 underline-offset-4 hover:underline"
          >
            {isRegister ? 'Đăng nhập' : 'Đăng ký ngay'}
          </Link>
        </p>
      </div>
    </main>
  );
}
