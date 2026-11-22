import React, { useState, useEffect, useRef, useCallback } from 'react';
import { message } from 'antd';
import { useTranslation } from '../../i18n';
import { sharingApi } from '../../services/apiClient';
import type { PublicDeck, PublicDeckPreview, StudyTarget } from '../../types';
import { deckMatchesTarget } from '../decks/deckScope';
import { sortUnits } from '../../utils/sorting';
import { Pagination } from '../../components/ui/Pagination';
import {
  SearchOutlined,
  CopyOutlined,
  EyeOutlined,
  CloseOutlined,
  CheckCircleOutlined,
  LoadingOutlined,
  BookOutlined
} from '@ant-design/icons';

interface PublicDecksViewProps {
  target: StudyTarget | null;
  onDeckCloned?: () => void;
}

export const PublicDecksView: React.FC<PublicDecksViewProps> = ({ target, onDeckCloned }) => {
  const { t } = useTranslation();
  const [decks, setDecks] = useState<PublicDeck[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [previewDeck, setPreviewDeck] = useState<PublicDeckPreview | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [cloningId, setCloningId] = useState<string | null>(null);
  const [clonedSuccess, setClonedSuccess] = useState<string | null>(null);
  const [loadError, setLoadError] = useState('');
  const [page, setPage] = useState(1);
  const loadRequestId = useRef(0);
  const pageCount = Math.max(1, Math.ceil(decks.length / 6));
  const visibleDecks = decks.slice((Math.min(page, pageCount) - 1) * 6, Math.min(page, pageCount) * 6);

  const loadPublicDecks = useCallback(async (search?: string) => {
    const requestId = ++loadRequestId.current;
    if (!target) { setDecks([]); setIsLoading(false); return; }
    setIsLoading(true);
    setLoadError('');
    try {
      const data = await sharingApi.getPublicDecks(search, target.languageId);
      if (requestId === loadRequestId.current) {
        setDecks(sortUnits(data.filter((deck) => deckMatchesTarget(deck, target))));
        setPage(1);
      }
    } catch (cause) {
      if (requestId === loadRequestId.current) {
        setDecks([]);
        setLoadError(cause instanceof Error ? cause.message : t('sharing.loadFailed'));
      }
    } finally {
      if (requestId === loadRequestId.current) setIsLoading(false);
    }
  }, [target, t]);

  useEffect(() => {
    void loadPublicDecks();
  }, [loadPublicDecks]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    loadPublicDecks(searchTerm);
  };

  const handlePreview = async (deckId: string) => {
    try {
      const preview = await sharingApi.getPublicDeckPreview(deckId);
      setPreviewDeck(preview);
    } catch (cause) {
      message.error(cause instanceof Error ? cause.message : t('common.error'));
    }
  };

  const handleClone = async (deckId: string) => {
    setCloningId(deckId);
    try {
      await sharingApi.cloneDeck(deckId);
      setClonedSuccess(deckId);
      message.success(t('sharing.clonedSuccess'));
      if (onDeckCloned) onDeckCloned();
      setTimeout(() => setClonedSuccess(null), 3000);
    } catch (cause) {
      message.error(cause instanceof Error ? cause.message : t('common.error'));
    } finally {
      setCloningId(null);
    }
  };

  return (
    <div className="space-y-6">
      {!target && <p className="rounded-xl border-notebook bg-white p-5 text-sm">{t('courses.selectTarget')}</p>}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-stone-900">{t('sharing.title')}</h1>
          <p className="text-xs text-stone-600">{t('sharing.subtitle')}</p>
        </div>

        <form onSubmit={handleSearch} className="flex gap-2">
          <div className="relative w-64">
            <SearchOutlined className="absolute left-3 top-3 text-stone-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder={t('sharing.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 bg-white border-notebook rounded-lg text-xs font-medium focus:outline-none focus:ring-2 focus:ring-amber-400"
            />
          </div>
          <button
            type="submit"
            className="px-3.5 py-2 bg-stone-900 text-white rounded-lg text-xs font-bold notebook-btn hover:bg-stone-800 transition cursor-pointer"
          >
            {t('common.all')}
          </button>
        </form>
      </div>

      {loadError && <p role="alert" className="text-sm text-rose-700">{loadError}</p>}
      {!isLoading && !loadError && decks.length === 0 && <p className="text-sm text-stone-600">{t('sharing.noDecks')}</p>}
      {isLoading ? (
        <div className="flex items-center justify-center min-h-[300px]">
          <LoadingOutlined className="text-3xl text-stone-500 animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {visibleDecks.map((deck) => (
            <div
              key={deck.id}
              className="bg-white border-notebook-2 rounded-2xl p-5 shadow-xs hover:shadow-md transition flex flex-col justify-between"
            >
              <div>
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <BookOutlined className="text-stone-600" />
                    <span className="font-bold text-sm text-stone-900">{deck.title}</span>
                  </div>
                </div>

                <p className="text-xs text-stone-600 line-clamp-3 mb-3 leading-relaxed">
                  {deck.description}
                </p>

                <div className="flex flex-wrap gap-1 mb-4">
                  {deck.tags.map((tag) => (
                    <span
                      key={tag}
                      className="text-[10px] font-mono font-semibold px-2 py-0.5 rounded bg-stone-100 text-stone-600 border border-stone-200"
                    >
                      #{tag}
                    </span>
                  ))}
                </div>
              </div>

              <div className="pt-3 border-t border-stone-200 space-y-3">
                <div className="flex items-center justify-between text-[11px] text-stone-500">
                  <span>
                    {deck.totalCards ?? deck.cardsCount ?? 0} {t('sharing.cardsCount')}
                  </span>
                  <span>
                    {deck.cloneCount ?? 0} {t('sharing.clones')}
                  </span>
                </div>

                <div className="flex gap-2">
                  <button
                    onClick={() => handlePreview(deck.id)}
                    className="flex-1 py-1.5 border-notebook rounded-lg bg-white hover:bg-stone-50 text-xs font-bold text-stone-700 flex items-center justify-center gap-1.5 transition"
                  >
                    <EyeOutlined />
                    <span>{t('sharing.previewCards')}</span>
                  </button>

                  <button
                    onClick={() => handleClone(deck.id)}
                    disabled={cloningId === deck.id}
                    className="flex-1 py-1.5 bg-[#E11D48] hover:bg-rose-700 text-white rounded-lg text-xs font-bold notebook-btn flex items-center justify-center gap-1.5 transition cursor-pointer disabled:opacity-50"
                  >
                    {clonedSuccess === deck.id ? (
                      <>
                        <CheckCircleOutlined />
                        <span>{t('sharing.clonedSuccess')}</span>
                      </>
                    ) : cloningId === deck.id ? (
                      <LoadingOutlined />
                    ) : (
                      <>
                        <CopyOutlined />
                        <span>{t('sharing.cloneDeck')}</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {!isLoading && <Pagination page={page} pageCount={pageCount} onChange={setPage} />}

      {previewDeck && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-lg bg-[#FAF6EE] border-notebook-2 shadow-2xl rounded-2xl p-6 relative">
            <button
              onClick={() => setPreviewDeck(null)}
              className="absolute top-4 right-4 text-stone-500 hover:text-stone-900 transition"
            >
              <CloseOutlined />
            </button>

            <h2 className="text-base font-bold text-stone-900 mb-1">{previewDeck.title}</h2>
            <p className="text-xs text-stone-600 mb-4">{previewDeck.description}</p>

            <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 mb-2">
              {t('sharing.previewCards')}
            </h3>

            <div className="divide-y divide-stone-200 max-h-[300px] overflow-y-auto bg-white border-notebook rounded-xl p-3 mb-4">
              {previewDeck.sampleCards.map((card) => (
                <div key={card.id} className="py-2.5">
                  <span className="font-bold text-xs text-stone-900 block">{card.front}</span>
                  {card.phonetic && <span className="text-xs text-stone-500">{card.phonetic}</span>}
                  <span className="text-xs text-stone-600 block mt-0.5">{card.meanings?.length ? card.meanings.join('; ') : card.back}</span>
                  {card.example && <span className="text-xs italic text-stone-500 block">{card.example}</span>}
                </div>
              ))}
            </div>

            <button
              onClick={() => {
                handleClone(previewDeck.id);
                setPreviewDeck(null);
              }}
              className="w-full py-2.5 bg-[#E11D48] text-white rounded-lg text-xs font-bold notebook-btn hover:bg-rose-700 transition cursor-pointer"
            >
              {t('sharing.cloneDeck')}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
