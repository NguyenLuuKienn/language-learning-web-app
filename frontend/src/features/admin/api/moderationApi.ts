import { apiRequest } from '../../../services/apiClient';
import type { AdminDeck, AdminUser, PagedResponse } from '../types/moderation.types';

function query(page: number, search: string) {
  const params = new URLSearchParams({ page: String(page), pageSize: '10' });
  if (search.trim()) params.set('search', search.trim());
  return params.toString();
}

export const moderationApi = {
  users: (page: number, search: string) =>
    apiRequest<PagedResponse<AdminUser>>(`/admin/moderation/users?${query(page, search)}`),
  decks: (page: number, search: string) =>
    apiRequest<PagedResponse<AdminDeck>>(`/admin/moderation/decks?${query(page, search)}`),
  setUserActive: (id: string, active: boolean) =>
    apiRequest<AdminUser>(`/admin/moderation/users/${encodeURIComponent(id)}/${active ? 'unlock' : 'lock'}`, { method: 'POST' }),
  setDeckHidden: (id: string, hidden: boolean) =>
    apiRequest<AdminDeck>(`/admin/moderation/decks/${encodeURIComponent(id)}/${hidden ? 'hide' : 'restore'}`, { method: 'POST' }),
};
