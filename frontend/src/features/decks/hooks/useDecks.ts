
import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';

import { decksApi } from '../api/decksApi';

import type {
  CreateDeckRequest,
  PaginationParams,
  UpdateDeckRequest,
} from '../types/deck.types';

export const deckQueryKeys = {
  all: ['decks'] as const,

list: (pagination: PaginationParams) =>
  [
    'decks',
    'list',
    pagination.page ?? 1,
    pagination.pageSize ?? 10,
    pagination.search?.trim() ?? '',
  ] as const,

  detail: (id: string) => ['decks', 'detail', id] as const,
};

export function useDecks(pagination: PaginationParams = {}) {
  return useQuery({
    queryKey: deckQueryKeys.list(pagination),
    queryFn: () => decksApi.getDecks(pagination),
  });
}

export function useDeckById(id: string) {
  return useQuery({
    queryKey: deckQueryKeys.detail(id),
    queryFn: () => decksApi.getDeckById(id),
    enabled: Boolean(id),
  });
}

export function useCreateDeck() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateDeckRequest) =>
      decksApi.createDeck(request),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: deckQueryKeys.all,
      }),
  });
}

export function useUpdateDeck() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateDeckRequest;
    }) => decksApi.updateDeck(id, request),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: deckQueryKeys.all,
      }),
  });
}

export function useDeleteDeck() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => decksApi.deleteDeck(id),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: deckQueryKeys.all,
      }),
  });
}
