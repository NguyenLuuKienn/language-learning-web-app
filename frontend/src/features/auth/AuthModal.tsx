import React, { useState } from 'react';
import { message } from 'antd';
import { useTranslation } from '../../i18n';
import { useAuth } from './useAuth';
import { CloseOutlined, LockOutlined, MailOutlined, UserOutlined } from '@ant-design/icons';

import type { UserProfile } from '../../types';

interface AuthModalProps {
  isOpen: boolean;
  onClose: () => void;
  defaultMode?: 'login' | 'register';
  onLoginSuccess?: (user: UserProfile) => void;
}

export const AuthModal: React.FC<AuthModalProps> = ({
  isOpen,
  onClose,
  defaultMode = 'login',
  onLoginSuccess
}) => {
  const { t, language } = useTranslation();
  const { login, register } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>(defaultMode);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (mode === 'register' && displayName.trim().length < 2) {
      setError(language === 'vi' ? 'Tên hiển thị cần ít nhất 2 ký tự.' : 'Display name needs at least 2 characters.');
      return;
    }
    setIsSubmitting(true);

    try {
      let loggedUser: UserProfile;
      if (mode === 'login') {
        loggedUser = await login(email.trim(), password);
      } else {
        loggedUser = await register(email.trim(), password, displayName.trim());
      }
      message.success(t('common.success'));
      onClose();
      if (onLoginSuccess && loggedUser) {
        onLoginSuccess(loggedUser);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : t('common.error');
      setError(msg);
      message.error(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
      <div role="dialog" aria-modal="true" aria-label={mode === 'login' ? t('auth.loginTitle') : t('auth.registerTitle')} className="w-full max-w-md bg-[#FAF6EE] border-notebook-2 shadow-2xl rounded-2xl overflow-hidden p-6 relative">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-stone-500 hover:text-stone-900 transition"
        >
          <CloseOutlined className="text-lg" />
        </button>

        <div className="mb-6 text-center">
          <span className="bg-[#E11D48] text-white text-xs font-mono font-bold px-2 py-0.5 rounded border border-[#9F1239] inline-block mb-2">
            LANGUAGE LEARNING
          </span>
          <h2 className="text-xl font-bold text-stone-900">
            {mode === 'login' ? t('auth.loginTitle') : t('auth.registerTitle')}
          </h2>
        </div>

        {error && (
          <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded-lg text-red-700 text-xs font-semibold">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === 'register' && (
            <div>
              <label className="block text-xs font-bold text-stone-700 mb-1">
                {t('auth.displayName')}
              </label>
              <div className="relative">
                <UserOutlined className="absolute left-3 top-3 text-stone-400" />
                <input
                  type="text"
                  required
                  minLength={2}
                  maxLength={100}
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  className="w-full pl-9 pr-3 py-2 text-sm bg-white border-notebook rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-400"
                  placeholder="Alex Rivers"
                />
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-bold text-stone-700 mb-1">
              {t('auth.email')}
            </label>
            <div className="relative">
              <MailOutlined className="absolute left-3 top-3 text-stone-400" />
              <input
                type="email"
                required
                maxLength={254}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full pl-9 pr-3 py-2 text-sm bg-white border-notebook rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-400"
                placeholder="user@example.com"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-stone-700 mb-1">
              {t('auth.password')}
            </label>
            <div className="relative">
              <LockOutlined className="absolute left-3 top-3 text-stone-400" />
              <input
                type="password"
                required
                minLength={mode === 'register' ? 15 : undefined}
                maxLength={128}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full pl-9 pr-3 py-2 text-sm bg-white border-notebook rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-400"
                placeholder="••••••••"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full py-2.5 bg-[#262626] text-white rounded-lg font-bold text-sm notebook-btn hover:bg-stone-800 disabled:opacity-50 transition cursor-pointer"
          >
            {isSubmitting
              ? t('common.loading')
              : mode === 'login'
                ? t('auth.submitLogin')
                : t('auth.submitRegister')}
          </button>
        </form>

        <div className="mt-6 text-center text-xs text-stone-600">
          {mode === 'login' ? (
            <button
              onClick={() => {
                setMode('register');
                setError(null);
              }}
              className="text-[#E11D48] font-bold hover:underline"
            >
              {t('auth.noAccount')}
            </button>
          ) : (
            <button
              onClick={() => {
                setMode('login');
                setError(null);
              }}
              className="text-[#E11D48] font-bold hover:underline"
            >
              {t('auth.haveAccount')}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
