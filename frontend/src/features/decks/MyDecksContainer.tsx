
import { useEffect, useMemo, useState } from 'react';

import { useDecks } from './hooks/useDecks';
import { useLanguages } from './hooks/useLanguages';
import { MyDecksWorkspace } from './MyDecksWorkspace';

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;

export function MyDecksContainer() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
    }, SEARCH_DEBOUNCE_MS);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [search]);

  const {
    data,
    isPending,
    isError,
    refetch,
  } = useDecks({
    page,
    pageSize: PAGE_SIZE,
    search: debouncedSearch,
  });

  const { data: languages = [] } = useLanguages();

  const languageNames = useMemo(
    () =>
      Object.fromEntries(
        languages.map((language) => [
          language.id,
          language.name,
        ])
      ),
    [languages]
  );

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  return (
    <MyDecksWorkspace
      decks={data?.items ?? []}
      languages={languages}
      search={search}
      onSearchChange={handleSearchChange}
      page={page}
      totalPages={data?.totalPages ?? 0}
      totalItems={data?.totalItems ?? 0}
      onPageChange={setPage}
      isLoading={isPending}
      isError={isError}
      onRetry={() => {
        void refetch();
      }}
      languageNames={languageNames}
    />
  );
}
