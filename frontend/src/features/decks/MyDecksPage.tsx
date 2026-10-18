
import { BookOpen, LoaderCircle, Plus, RefreshCw } from 'lucide-react';

import { DeckCard } from './components/DeckCard';
import { DeckSearch } from './components/DeckSearch';

import type { DeckResponse } from './types/deck.types';

interface MyDecksPageProps {
  decks: DeckResponse[];
  search: string;
  onSearchChange: (value: string) => void;
  onCreateDeck: () => void;
  onEditDeck: (deck: DeckResponse) => void;
  page: number;
  totalPages: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  languageNames?: Record<string, string>;
}

export function MyDecksPage({
  decks,
  search,
  onSearchChange,
  onCreateDeck,
  onEditDeck,
  page,
  totalPages,
  totalItems,
  onPageChange,
  isLoading,
  isError,
  onRetry,
  languageNames = {},
}: MyDecksPageProps) {
  return (
    <main className="min-h-screen bg-[#FAF6EE] px-4 py-8 text-stone-900 sm:px-6 lg:px-10">
      <div className="mx-auto max-w-7xl">
        <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="mb-2 text-xs font-bold uppercase tracking-widest text-rose-600">
              MY LIBRARY
            </p>

            <h1 className="text-3xl font-extrabold tracking-tight sm:text-4xl">
              Bộ thẻ của tôi
            </h1>

            <p className="mt-2 text-sm text-stone-500">
              Quản lý và tổ chức các bộ thẻ học tập.
            </p>
          </div>

          <button
            type="button"
            onClick={onCreateDeck}
            className="inline-flex items-center justify-center gap-2 rounded-xl border-2 border-stone-900 bg-stone-900 px-5 py-3 text-sm font-bold text-white transition hover:bg-stone-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-600"
          >
            <Plus size={18} />
            Tạo bộ thẻ
          </button>
        </header>

        <section
          aria-label="Tìm kiếm bộ thẻ"
          className="mt-8 rounded-xl border-2 border-stone-800 bg-white p-4 sm:p-5"
        >
          <DeckSearch
            value={search}
            onChange={onSearchChange}
            placeholder="Tìm kiếm theo tên bộ thẻ..."
          />

          <p className="mt-3 text-xs text-stone-500">
            {totalItems} bộ thẻ
          </p>
        </section>

        <section aria-label="Danh sách bộ thẻ" className="mt-6">
          {isLoading ? (
            <div
              role="status"
              className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed border-stone-300 bg-white"
            >
              <LoaderCircle
                size={30}
                className="animate-spin text-rose-600"
              />
              <p className="text-sm text-stone-500">
                Đang tải bộ thẻ...
              </p>
            </div>
          ) : isError ? (
            <div
              role="alert"
              className="flex min-h-64 flex-col items-center justify-center gap-4 rounded-xl border-2 border-dashed border-red-200 bg-white p-6 text-center"
            >
              <p className="font-semibold text-red-700">
                Không thể tải danh sách bộ thẻ.
              </p>

              <button
                type="button"
                onClick={onRetry}
                className="inline-flex items-center gap-2 rounded-lg bg-stone-900 px-4 py-2 text-sm font-semibold text-white hover:bg-stone-700"
              >
                <RefreshCw size={16} />
                Thử lại
              </button>
            </div>
          ) : decks.length === 0 ? (
            <div className="flex min-h-64 flex-col items-center justify-center rounded-xl border-2 border-dashed border-stone-300 bg-white px-6 py-10 text-center">
              <BookOpen size={36} className="text-stone-400" />

              <h2 className="mt-4 text-lg font-bold">
                {search.trim()
                  ? 'Không tìm thấy bộ thẻ phù hợp'
                  : 'Chưa có bộ thẻ nào'}
              </h2>

              <p className="mt-2 max-w-md text-sm text-stone-500">
                {search.trim()
                  ? 'Hãy thử một từ khóa tìm kiếm khác.'
                  : 'Tạo bộ thẻ đầu tiên để bắt đầu tổ chức nội dung học tập.'}
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
              {decks.map((deck) => (
                <DeckCard
                  key={deck.id}
                  deck={deck}
                  languageName={languageNames[deck.languageId]}
                  onEdit={onEditDeck}
                />
              ))}
            </div>
          )}
        </section>

        {!isLoading && !isError && totalPages > 1 && (
          <nav
            aria-label="Phân trang bộ thẻ"
            className="mt-8 flex flex-wrap items-center justify-center gap-4"
          >
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => onPageChange(page - 1)}
              className="rounded-lg border-2 border-stone-800 bg-white px-4 py-2 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-40"
            >
              Trang trước
            </button>

            <span className="text-sm font-medium text-stone-600">
              Trang {page} / {totalPages}
            </span>

            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => onPageChange(page + 1)}
              className="rounded-lg border-2 border-stone-800 bg-white px-4 py-2 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-40"
            >
              Trang sau
            </button>
          </nav>
        )}
      </div>
    </main>
  );
}
