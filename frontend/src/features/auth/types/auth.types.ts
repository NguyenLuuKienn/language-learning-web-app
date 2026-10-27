export type AuthRole = 'Learner' | 'Admin';

export interface AuthUser {
  id: string;
  email: string;
  role: AuthRole;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  displayName: string;
}

export interface AuthSnapshot {
  accessToken: string | null;
  user: AuthUser | null;
}
