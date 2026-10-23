
import { apiRequest } from '../../../services/apiClient';

import type {
  CreateDeckRequest,
  DeckResponse,
  PagedResponse,
  PaginationParams,
  UpdateDeckRequest,
} from '../types/deck.types';

const DECKS_ENDPOINT = '/decks';

export const decksApi = {
  getDecks: async (
    pagination: PaginationParams = {}
  ): Promise<PagedResponse<DeckResponse>> => {


  const params = new URLSearchParams({
    page: String(pagination.page ?? 1),
    pageSize: String(pagination.pageSize ?? 10),
    onlyMine: 'true',
  });

  const search = pagination.search?.trim();

  if (search) {
    params.set('search', search);
  }

    return apiRequest<PagedResponse<DeckResponse>>(
      `${DECKS_ENDPOINT}?${params.toString()}`
    );
  },

  getDeckById: async (id: string): Promise<DeckResponse> => {
    return apiRequest<DeckResponse>(
      `${DECKS_ENDPOINT}/${encodeURIComponent(id)}`
    );
  },

  createDeck: async (
    request: CreateDeckRequest
  ): Promise<DeckResponse> => {
    return apiRequest<DeckResponse>(DECKS_ENDPOINT, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

    updateDeck: async (
    id: string,
    request: UpdateDeckRequest
  ): Promise<DeckResponse> => {
    return apiRequest<DeckResponse>(
      `${DECKS_ENDPOINT}/${encodeURIComponent(id)}`,
      {
        method: 'PATCH',
        body: JSON.stringify(request),
      }
    );
  },

  deleteDeck: async (id: string): Promise<void> => {
    return apiRequest<void>(
      `${DECKS_ENDPOINT}/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
      }
    );
  },
};
