import { apiRequest } from '../../../services/apiClient';

import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
} from '../types/auth.types';

export const authApi = {
  login(request: LoginRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>(
      '/auth/login',
      { method: 'POST', body: JSON.stringify(request) },
      { anonymous: true }
    );
  },

  register(request: RegisterRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>(
      '/auth/register',
      { method: 'POST', body: JSON.stringify(request) },
      { anonymous: true }
    );
  },

  logout(refreshToken: string): Promise<void> {
    return apiRequest<void>(
      '/auth/logout',
      { method: 'POST', body: JSON.stringify({ refreshToken }) },
      { anonymous: true }
    );
  },
};
