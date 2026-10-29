import { useEffect, useState, useSyncExternalStore } from 'react';
import { useQueryClient } from '@tanstack/react-query';

import { db } from '../../offline/db';
import { refreshSession } from '../../services/apiClient';
import { authApi } from './api/authApi';
import { AuthContext } from './authContext';
import {
  clearAuthSession,
  getAuthSnapshot,
  getRefreshToken,
  saveAuthSession,
  subscribeToAuth,
} from './authSession';

import type { ReactNode } from 'react';
import type { LoginRequest, RegisterRequest } from './types/auth.types';

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const session = useSyncExternalStore(
    subscribeToAuth,
    getAuthSnapshot,
    getAuthSnapshot
  );
  const [isRestoring, setIsRestoring] = useState(
    () => getRefreshToken() !== null
  );

  useEffect(() => {
    let active = true;

    try {
      window.localStorage.removeItem('access_token');
    } catch {
      // Legacy storage may be unavailable.
    }

    if (getRefreshToken()) {
      void refreshSession().finally(() => {
        if (active) {
          setIsRestoring(false);
        }
      });
    }

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!session.user) {
      queryClient.clear();
    }
  }, [queryClient, session.user]);

  async function login(request: LoginRequest) {
    const response = await authApi.login(request);
    saveAuthSession(response);
  }

  async function register(request: RegisterRequest) {
    const response = await authApi.register(request);
    saveAuthSession(response);
  }

  async function logout(): Promise<{ revoked: boolean; offlineCleared: boolean }> {
    const refreshToken = getRefreshToken();
    let revoked = true;
    let offlineCleared = true;

    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } catch {
      revoked = false;
    }

    clearAuthSession();
    queryClient.clear();

    try {
      await Promise.all(db.tables.map((table) => table.clear()));
    } catch {
      try {
        await db.delete();
      } catch {
        offlineCleared = false;
      }
    }

    return { revoked, offlineCleared };
  }

  return (
    <AuthContext.Provider
      value={{ ...session, isRestoring, login, register, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}
