import React, { useState } from 'react';
import { message } from 'antd';
import { useTranslation } from '../../i18n';
import { useAuth } from '../auth';
import { authApi } from '../../services/apiClient';
import { NotebookSelect } from '../../components/ui/NotebookSelect';
import {
  CloseOutlined,
  SettingOutlined,
  GlobalOutlined,
  LockOutlined,
  FieldTimeOutlined,
  CheckCircleOutlined
} from '@ant-design/icons';

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({ isOpen, onClose }) => {
  const { t, language, setLanguage } = useTranslation();
  const { user, updateGoal, logout } = useAuth();

  const [dailyGoal, setDailyGoal] = useState<number>(user?.dailyGoal || 20);
  const [timezone, setTimezone] = useState<string>(
    user?.timezone || Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
  );
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [isSavingGoal, setIsSavingGoal] = useState(false);
  const [isChangingPass, setIsChangingPass] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSavePreferences = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSavingGoal(true);
    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      await updateGoal(dailyGoal, timezone);
      setSuccessMessage(t('common.success'));
      message.success(t('common.success'));
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : t('common.error');
      setErrorMessage(msg);
      message.error(msg);
    } finally {
      setIsSavingGoal(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!oldPassword || !newPassword) return;

    setIsChangingPass(true);
    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      await authApi.changePassword({
        currentPassword: oldPassword,
        newPassword: newPassword
      });
      setOldPassword('');
      setNewPassword('');
      onClose();
      await logout();
      message.success(t('auth.passwordChangedSignIn'));
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : t('common.error');
      setErrorMessage(msg);
      message.error(msg);
    } finally {
      setIsChangingPass(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
      <div className="w-full max-w-lg bg-[#FAF6EE] border-notebook-2 shadow-2xl rounded-2xl p-6 relative max-h-[90vh] overflow-y-auto">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-stone-500 hover:text-stone-900 transition"
        >
          <CloseOutlined className="text-lg" />
        </button>

        <div className="flex items-center gap-2 mb-4">
          <SettingOutlined className="text-xl text-stone-700" />
          <h2 className="text-lg font-bold text-stone-900">{t('nav.settings')}</h2>
        </div>

        {successMessage && (
          <div className="mb-4 p-3 bg-emerald-100 border border-emerald-300 rounded-lg text-emerald-800 text-xs font-semibold flex items-center gap-2">
            <CheckCircleOutlined />
            <span>{successMessage}</span>
          </div>
        )}

        {errorMessage && (
          <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded-lg text-red-700 text-xs font-semibold">
            {errorMessage}
          </div>
        )}

        <div className="bg-white border-notebook rounded-xl p-4 mb-4 space-y-2">
          <div className="flex items-center gap-2 text-stone-800 text-xs font-bold">
            <GlobalOutlined />
            <span>{t('common.language')}</span>
          </div>
          <div className="grid grid-cols-2 gap-2 pt-1">
            <button
              type="button"
              onClick={() => setLanguage('en')}
              className={`py-2 rounded-lg text-xs font-bold border-notebook transition ${
                language === 'en'
                  ? 'bg-amber-100 text-amber-900 font-black shadow-2xs'
                  : 'bg-white text-stone-600 hover:bg-stone-50'
              }`}
            >
              {t('common.english')}
            </button>
            <button
              type="button"
              onClick={() => setLanguage('vi')}
              className={`py-2 rounded-lg text-xs font-bold border-notebook transition ${
                language === 'vi'
                  ? 'bg-amber-100 text-amber-900 font-black shadow-2xs'
                  : 'bg-white text-stone-600 hover:bg-stone-50'
              }`}
            >
              {t('common.vietnamese')}
            </button>
          </div>
        </div>

        <form onSubmit={handleSavePreferences} className="bg-white border-notebook rounded-xl p-4 mb-4 space-y-3">
          <div className="flex items-center gap-2 text-stone-800 text-xs font-bold">
            <FieldTimeOutlined />
            <span>{t('auth.saveSettings')}</span>
          </div>

          <div>
            <label className="block text-[11px] font-bold text-stone-600 mb-1">
              {t('auth.dailyGoal')}
            </label>
            <input
              type="number"
              min={5}
              max={200}
              value={dailyGoal}
              onChange={(e) => setDailyGoal(Number(e.target.value))}
              className="w-full px-3 py-1.5 text-xs bg-stone-50 border-notebook rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-400"
            />
          </div>

          <div>
            <label className="block text-[11px] font-bold text-stone-600 mb-1">
              {t('auth.timezone')}
            </label>
            <NotebookSelect
              value={timezone}
              onChange={(val) => setTimezone(val)}
              options={[
                { value: 'UTC', label: 'UTC (Universal Coordinated Time)' },
                { value: 'Asia/Ho_Chi_Minh', label: 'Asia/Ho_Chi_Minh (UTC+7)' },
                { value: 'Asia/Tokyo', label: 'Asia/Tokyo (UTC+9)' },
                { value: 'America/New_York', label: 'America/New_York (UTC-5)' },
                { value: 'Europe/London', label: 'Europe/London (UTC+0)' }
              ]}
              className="w-full"
            />
          </div>

          <button
            type="submit"
            disabled={isSavingGoal}
            className="w-full py-2 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-xs font-bold notebook-btn transition cursor-pointer"
          >
            {isSavingGoal ? t('common.loading') : t('auth.saveSettings')}
          </button>
        </form>

        <form onSubmit={handleChangePassword} className="bg-white border-notebook rounded-xl p-4 space-y-3">
          <div className="flex items-center gap-2 text-stone-800 text-xs font-bold">
            <LockOutlined />
            <span>{t('auth.changePassword')}</span>
          </div>

          <div>
            <label className="block text-[11px] font-bold text-stone-600 mb-1">
              {t('auth.oldPassword')}
            </label>
            <input
              type="password"
              required
              value={oldPassword}
              onChange={(e) => setOldPassword(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-stone-50 border-notebook rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-400"
            />
          </div>

          <div>
            <label className="block text-[11px] font-bold text-stone-600 mb-1">
              {t('auth.newPassword')}
            </label>
            <input
              type="password"
              required
              value={newPassword}
              minLength={15}
              maxLength={128}
              onChange={(e) => setNewPassword(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-stone-50 border-notebook rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-400"
            />
          </div>

          <button
            type="submit"
            disabled={isChangingPass}
            className="w-full py-2 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-xs font-bold notebook-btn transition cursor-pointer"
          >
            {isChangingPass ? t('common.loading') : t('auth.changePassword')}
          </button>
        </form>
      </div>
    </div>
  );
};
