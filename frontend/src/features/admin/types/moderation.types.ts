export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
}

export interface AdminDeck {
  id: string;
  title: string;
  ownerId: string;
  isPublic: boolean;
  isModerated: boolean;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
