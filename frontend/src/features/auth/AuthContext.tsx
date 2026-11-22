import React, { createContext, useState, useEffect, type ReactNode } from 'react';
import { authApi } from '../../services/apiClient';
import type { UserProfile } from '../../types';

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<UserProfile>;
  register: (email: string, password: string, displayName: string) => Promise<UserProfile>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
  updateGoal: (dailyGoal: number, timezone: string) => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

const normalizeUser = (rawUser: unknown): UserProfile | null => {
  if (!rawUser || typeof rawUser !== 'object') return null;
  const u = rawUser as Record<string, unknown>;
  const roleVal = String(u.role ?? '').trim().toLowerCase();
  const isAdmin = roleVal === 'admin' || roleVal === '2';
  return {
    ...(u as unknown as UserProfile),
    role: isAdmin ? 'Admin' : 'Learner',
    dailyGoal: Number(u.dailyTarget ?? u.dailyGoal ?? 10)
  };
};

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      localStorage.removeItem('access_token');
      localStorage.removeItem('token');
      localStorage.removeItem('refresh_token');
      localStorage.removeItem('user_profile');
      try {
        const profile = await authApi.getMe();
        setUser(normalizeUser(profile));
        setToken('cookie-session');
      } catch {
        setToken(null);
        setUser(null);
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email: string, password: string) => {
    const response = await authApi.login({ email, password });
    const normalized = normalizeUser(response.user);
    setToken('cookie-session');
    setUser(normalized);
    return normalized!;
  };

  const register = async (email: string, password: string, displayName: string) => {
    const response = await authApi.register({ email, password, displayName });
    const normalized = normalizeUser(response.user);
    setToken('cookie-session');
    setUser(normalized);
    return normalized!;
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
    }
    localStorage.removeItem('access_token');
    localStorage.removeItem('token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user_profile');
    setToken(null);
    setUser(null);
  };

  const refreshProfile = async () => {
    try {
      const profile = await authApi.getMe();
      const normalized = normalizeUser(profile);
      setUser(normalized);
    } catch {
    }
  };

  const updateGoal = async (dailyGoal: number, timezone: string) => {
    const updated = await authApi.updateTimezone(timezone);
    await authApi.updateDailyGoal(dailyGoal);
    const normalized = normalizeUser(updated);
    setUser(normalized ? { ...normalized, dailyGoal } : null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token && !!user,
        isLoading,
        login,
        register,
        logout,
        refreshProfile,
        updateGoal
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
