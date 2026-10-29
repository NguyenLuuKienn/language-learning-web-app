import { Navigate, useLocation } from 'react-router-dom';

import { useAuth } from './authContext';

import type { ReactNode } from 'react';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, isRestoring } = useAuth();
  const location = useLocation();

  if (isRestoring) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#FAF6EE] text-stone-600">
        Đang khôi phục phiên đăng nhập...
      </div>
    );
  }

  if (!user) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: `${location.pathname}${location.search}${location.hash}` }}
      />
    );
  }

  return children;
}
