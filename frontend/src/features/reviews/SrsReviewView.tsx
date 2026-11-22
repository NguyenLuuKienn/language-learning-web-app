import React, { useCallback, useEffect, useRef, useState } from 'react';
import { App } from 'antd';
import { CheckCircleOutlined, PlusOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import { SearchableSelect } from '../../components/ui/SearchableSelect';
import { decksApi, getErrorMessage, srsApi } from '../../services/apiClient';
import { speechService } from '../../services/speechService';
import type { Deck, Flashcard, SrsRating, StudyTarget } from '../../types';
import { deckMatchesTarget } from '../decks/deckScope';

interface SrsReviewViewProps {
  onComplete?: () => void;
  activeDeckId?: string;
  target?: StudyTarget | null;
  onLearnNew?: () => void;
}

const ratings: SrsRating[] = ['Again', 'Hard', 'Good', 'Easy'];

export const SrsReviewView: React.FC<SrsReviewViewProps> = ({ onComplete, onLearnNew, activeDeckId, target }) => {
  const { message } = App.useApp();
  const { t, language } = useTranslation();
  const [decks, setDecks] = useState<Deck[]>([]);
  const [deckId, setDeckId] = useState('');
  const [lessonId, setLessonId] = useState('');
  const [lessonSources, setLessonSources] = useState<{ lessonId: string; lessonTitle: string; unitTitle: string; cardCount: number }[]>([]);
  const [queue, setQueue] = useState<'due' | 'new'>('due');
  const [cards, setCards] = useState<Flashcard[]>([]);
  const [index, setIndex] = useState(0);
  const [flipped, setFlipped] = useState(false);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [reviewed, setReviewed] = useState(0);
  const [overview, setOverview] = useState<{ dueCount: number; learningCount: number; unseenCount: number; reviewCount: number; relearningCount: number; dueForecast7Days: number[]; totalCount: number; observedDueReviews: number; observedRecallPercent: number | null; nextDueAt?: string | null } | null>(null);
  const [addOpen, setAddOpen] = useState(false);
  const [front, setFront] = useState('');
  const [back, setBack] = useState('');
  const [addDeckId, setAddDeckId] = useState('');
  const decksRequestId = useRef(0);
  const dueRequestId = useRef(0);

  useEffect(() => {
    const requestId = ++decksRequestId.current;
    if (!target) {
      setDecks([]);
      setDeckId('');
      setLessonId('');
      setLessonSources([]);
      setAddDeckId('');
      setCards([]);
      setLoading(false);
      return;
    }
    decksApi.getDecks().then((items) => {
      if (requestId !== decksRequestId.current) return;
      const allowed = items.filter((item) => deckMatchesTarget(item, target));
      setDecks(allowed);
      const selected = allowed.find((item) => item.id === activeDeckId)?.id || '';
      setDeckId(selected);
      setAddDeckId(selected || allowed[0]?.id || '');
    }).catch((cause) => {
      if (requestId === decksRequestId.current) message.error(getErrorMessage(cause, t('common.error')));
    });
    srsApi.getLessonSources().then((items) => {
      if (requestId === decksRequestId.current) setLessonSources(items);
    }).catch((cause) => {
      if (requestId === decksRequestId.current) message.error(getErrorMessage(cause, t('common.error')));
    });
    return () => { decksRequestId.current++; };
  }, [message, t, target, activeDeckId]);

  const loadDue = useCallback(async () => {
    const requestId = ++dueRequestId.current;
    if (!target) { setCards([]); setOverview(null); setLoading(false); return; }
    setLoading(true);
    setError('');
    try {
      const [result, counts] = await Promise.all([
        srsApi.getDueCards(deckId || undefined, 30, queue === 'due', queue === 'new', lessonId || undefined),
        srsApi.getOverview(deckId || undefined, lessonId || undefined)
      ]);
      if (requestId !== dueRequestId.current) return;
      setCards(result.cards || []);
      setOverview(counts);
      setIndex(0);
      setFlipped(false);
      setReviewed(0);
    } catch (cause) {
      if (requestId !== dueRequestId.current) return;
      setCards([]);
      setError(cause instanceof Error ? cause.message : t('common.error'));
    } finally {
      if (requestId === dueRequestId.current) setLoading(false);
    }
    }, [deckId, lessonId, queue, target, t]);

  useEffect(() => { void loadDue(); }, [loadDue]);

  const card = cards[index];
  const currentDeck = decks.find((item) => item.id === card?.deckId);
  const speak = (value: string) => speechService.speak(value, currentDeck?.languageCode || target?.languageCode || 'en', { rate: 0.9 });

  const review = async (rating: SrsRating) => {
    if (!card || submitting) return;
    setSubmitting(true);
    try {
      await srsApi.submitReview(card.id, rating, 3000);
      void srsApi.getOverview(deckId || undefined, lessonId || undefined).then(setOverview).catch(() => undefined);
      setReviewed((count) => count + 1);
      setFlipped(false);
      if (index + 1 < cards.length) setIndex(index + 1);
      else { setIndex(cards.length); onComplete?.(); }
    } catch (cause) {
      message.error(getErrorMessage(cause, t('common.error')));
    } finally {
      setSubmitting(false);
    }
  };

  const addCard = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!addDeckId || !front.trim() || !back.trim()) return;
    try {
      await decksApi.createCard(addDeckId, { front: front.trim(), back: back.trim() });
      setFront(''); setBack(''); setAddOpen(false);
      message.success(t('srs.cardAdded'));
      await loadDue();
    } catch (cause) { message.error(getErrorMessage(cause, t('common.error'))); }
  };

  if (!target) return <div className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</div>;

  return <div className="mx-auto max-w-2xl space-y-5">
    <div className="rounded-xl border-notebook bg-white p-5">
      <h1 className="text-xl font-bold">{language === 'vi' ? 'Trung tâm ghi nhớ' : 'Memory center'}</h1>
      <p className="mt-1 text-xs text-stone-600">{t('srs.reviewDescription')}</p>
      <div className="mt-4 grid grid-cols-2 gap-2 sm:grid-cols-4">
        {[
          { value: overview?.unseenCount, label: language === 'vi' ? 'Từ mới' : 'New' },
          { value: overview?.learningCount, label: language === 'vi' ? 'Đang học' : 'Learning' },
          { value: overview?.reviewCount, label: language === 'vi' ? 'Đang ôn' : 'Review' },
          { value: overview?.relearningCount, label: language === 'vi' ? 'Học lại' : 'Relearning' }
        ].map((item) => <div key={item.label} className="rounded-xl border border-stone-200 bg-[#FAF6EE] p-3">
          <strong className="block text-lg">{item.value ?? '—'}</strong>
          <span className="text-xs text-stone-600">{item.label}</span>
        </div>)}
      </div>
      <p className="mt-4 text-sm font-bold">{language === 'vi' ? 'Đến hạn hôm nay' : 'Due today'}: {overview?.dueCount ?? '—'}</p>
      <p className="mt-2 text-xs text-stone-600">{overview?.observedRecallPercent == null
        ? (language === 'vi' ? 'Chưa đủ lượt ôn đến hạn để đo tỷ lệ nhớ.' : 'No due reviews recorded yet for recall measurement.')
        : (language === 'vi' ? `Chọn Good/Easy khi ôn đến hạn: ${overview.observedRecallPercent}% trên ${overview.observedDueReviews} lượt đã ghi nhận.` : `Good/Easy on due reviews: ${overview.observedRecallPercent}% across ${overview.observedDueReviews} recorded reviews.`)}</p>
      {overview?.dueForecast7Days?.length === 7 && <div className="mt-4"><p className="mb-2 text-xs font-bold">{language === 'vi' ? 'Dự báo 7 ngày tới' : 'Next 7 days'}</p><div className="grid grid-cols-7 gap-1" role="list" aria-label={language === 'vi' ? 'Dự báo số thẻ đến hạn' : 'Due card forecast'}>{overview.dueForecast7Days.map((count, day) => <div key={day} role="listitem" className="rounded-lg bg-[#FAF6EE] p-2 text-center text-xs"><strong className="block text-sm">{count}</strong><span>{new Date(Date.now() + day * 86400000).toLocaleDateString(language === 'vi' ? 'vi-VN' : 'en-US', { weekday: 'short' })}</span></div>)}</div></div>}
      <div className="mt-4 flex justify-end">
        <button onClick={onLearnNew} className="rounded-lg bg-amber-300 px-4 py-2 text-xs font-bold">{t('srs.learnNewWords')}</button>
      </div>
    </div>
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border-notebook bg-white p-4">
      <div className="min-w-0 flex-1"><SearchableSelect value={deckId} onChange={setDeckId} options={[{ value: '', label: t('srs.allDecks') }, ...decks.map((item) => ({ value: item.id, label: item.title }))]} label={t('srs.studyDeck')} /></div>
      <div className="min-w-0 flex-1"><SearchableSelect value={lessonId} onChange={setLessonId} options={[{ value: '', label: language === 'vi' ? 'Tất cả bài học' : 'All lessons' }, ...lessonSources.map((item) => ({ value: item.lessonId, label: `${item.unitTitle} · ${item.lessonTitle} (${item.cardCount})` }))]} label={language === 'vi' ? 'Bài học' : 'Lesson'} /></div>
      <button onClick={() => setAddOpen(true)} className="rounded-lg bg-amber-300 px-3 py-2 text-xs font-bold"><PlusOutlined /> {t('srs.quickAdd')}</button>
    </div>

    <div className="grid grid-cols-2 gap-2 rounded-xl border-notebook bg-white p-2 text-sm font-bold">
      <button type="button" aria-pressed={queue === 'due'} onClick={() => setQueue('due')}
        className={`rounded-lg px-3 py-2 ${queue === 'due' ? 'bg-amber-300' : 'hover:bg-stone-100'}`}>
        {t('srs.dueNow')} ({overview?.dueCount ?? '—'})
      </button>
      <button type="button" aria-pressed={queue === 'new'} onClick={() => setQueue('new')}
        className={`rounded-lg px-3 py-2 ${queue === 'new' ? 'bg-amber-300' : 'hover:bg-stone-100'}`}>
        {t('srs.unseenCount')} ({overview?.unseenCount ?? '—'})
      </button>
    </div>

    {loading && <p className="rounded-xl border-notebook bg-white p-8 text-center text-sm">{t('common.loading')}</p>}
    {!loading && error && <div role="alert" className="rounded-xl border border-rose-300 bg-rose-50 p-5 text-sm text-rose-700">{error}<button onClick={loadDue} className="ml-3 underline">{t('srs.checkAgain')}</button></div>}
    {!loading && !error && !card && <div className="rounded-xl border-notebook bg-white p-8 text-center space-y-3">
      {reviewed > 0 && <CheckCircleOutlined className="text-3xl text-emerald-600" />}
      <h2 className="font-bold">{reviewed > 0 ? t('srs.sessionComplete') : queue === 'due' ? t('srs.noDueCards') : (language === 'vi' ? 'Chưa có thẻ mới trong bộ lọc này' : 'No new cards in this selection')}</h2>
      {reviewed > 0 && <p className="text-xs text-stone-600">{language === 'vi' ? 'Đã xong lượt hiện tại. Tải lại để kiểm tra các thẻ còn lại.' : 'Current batch complete. Refresh to check for remaining cards.'}</p>}
      {reviewed === 0 && <p className="text-xs text-stone-600">{queue === 'due' ? (language === 'vi' ? 'Các từ đã học sẽ xuất hiện tại đây khi đến hạn.' : 'Learned words appear here when they are due.') : (language === 'vi' ? 'Thẻ tự tạo chưa có lịch ôn sẽ xuất hiện tại đây.' : 'Custom cards without a review schedule appear here.')}</p>}
      {reviewed === 0 && overview?.nextDueAt && <p className="text-xs text-stone-600">{t('srs.nextReview')}: {new Date(overview.nextDueAt).toLocaleString()}</p>}
      {reviewed > 0 && <p className="text-xs">{t('srs.reviewedCount', { count: reviewed })}</p>}
      <button onClick={loadDue} className="rounded-lg border-notebook px-3 py-2 text-xs">{t('srs.checkAgain')}</button>
    </div>}
    {!loading && card && <div className="space-y-3">
      <p className="text-xs font-bold text-stone-500">{t('srs.cardProgress', { current: index + 1, total: cards.length })} · {currentDeck?.title || ''} · {card.schedule ? t('srs.dueNow') : t('srs.newCard')}</p>
      <button type="button" onClick={() => setFlipped(!flipped)} className="flex min-h-64 w-full cursor-pointer flex-col items-center justify-center gap-4 rounded-2xl border-notebook-2 bg-white p-8 text-center">
        <span className="text-xs font-bold text-stone-500">{flipped ? t('srs.back') : t('srs.front')}</span>
        <strong className="text-2xl">{flipped ? (card.meanings?.length ? card.meanings.join('; ') : card.back) : card.front}</strong>
        {flipped && card.phonetic && <span className="text-sm text-stone-500">{card.phonetic}</span>}
        {flipped && card.partOfSpeech && card.partOfSpeech !== 'vocabulary' && <span className="text-xs text-stone-500">{card.partOfSpeech}</span>}
        {flipped && card.example && <span className="text-sm italic text-stone-600">{card.example}</span>}
        <span className="text-xs text-stone-500">{t('srs.flipPrompt')}</span>
      </button>
      <button type="button" onClick={() => speak(card.front)} aria-label={t('srs.listen')} className="rounded-lg border border-stone-300 bg-white px-3 py-2"><SoundOutlined /> {t('srs.listen')}</button>
      {flipped && <div className="grid grid-cols-4 gap-2 rounded-xl border-notebook bg-white p-3">
        {ratings.map((rating, ratingIndex) => <button key={rating} disabled={submitting} onClick={() => review(rating)} className="rounded-lg border border-stone-300 px-2 py-3 text-xs font-bold hover:bg-amber-100 disabled:opacity-50">{ratingIndex + 1}. {t(`srs.rating${rating}`)}</button>)}
      </div>}
    </div>}

    {addOpen && <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"><form onSubmit={addCard} className="w-full max-w-md space-y-3 rounded-xl border-notebook-2 bg-white p-5">
      <h2 className="font-bold">{t('srs.quickAdd')}</h2>
      <SearchableSelect value={addDeckId} onChange={setAddDeckId} options={decks.map((item) => ({ value: item.id, label: item.title }))} label={t('srs.chooseDeck')} />
      <input required value={front} onChange={(event) => setFront(event.target.value)} placeholder={t('srs.front')} className="w-full rounded-lg border border-stone-300 p-2 text-sm" />
      <textarea required value={back} onChange={(event) => setBack(event.target.value)} placeholder={t('srs.back')} className="w-full rounded-lg border border-stone-300 p-2 text-sm" />
      <div className="flex justify-end gap-2"><button type="button" onClick={() => setAddOpen(false)} className="rounded-lg border border-stone-300 px-3 py-2 text-xs">{t('common.cancel')}</button><button type="submit" className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white">{t('srs.saveCard')}</button></div>
    </form></div>}
  </div>;
};
