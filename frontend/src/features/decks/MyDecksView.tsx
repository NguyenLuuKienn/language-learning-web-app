import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { message, Modal } from 'antd';
import { BookOutlined, DeleteOutlined, DownloadOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons';
import { decksApi, coursesApi, getErrorMessage } from '../../services/apiClient';
import { useAuth } from '../auth';
import { useTranslation } from '../../i18n';
import type { Deck, Flashcard, Language, StudyTarget } from '../../types';
import { deckMatchesTarget } from './deckScope';
import { SearchableSelect } from '../../components/ui/SearchableSelect';
import { sortUnits } from '../../utils/sorting';

interface Props {
  target?: StudyTarget | null;
  onStudyDeck: (deckId: string) => void;
  onOpenCsvImport: (deckId: string) => void;
}

export function MyDecksView({ target, onStudyDeck, onOpenCsvImport }: Props) {
  const { user } = useAuth();
  const { language, t } = useTranslation();
  const isVi = language === 'vi';
  const [decks, setDecks] = useState<Deck[]>([]);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [selectedDeck, setSelectedDeck] = useState<Deck | null>(null);
  const [cards, setCards] = useState<Flashcard[]>([]);
  const [loading, setLoading] = useState(true);
  const loadRequestId = useRef(0);
  const [filterCode, setFilterCode] = useState(target?.languageCode ?? 'all');
  const [newTitle, setNewTitle] = useState('');
  const [newDescription, setNewDescription] = useState('');
  const [newLanguageId, setNewLanguageId] = useState(target?.languageId ?? '');
  const [newIsPublic, setNewIsPublic] = useState(false);
  const [newFront, setNewFront] = useState('');
  const [newBack, setNewBack] = useState('');
  const [newPhonetic, setNewPhonetic] = useState('');
  const [newPartOfSpeech, setNewPartOfSpeech] = useState('');
  const [newMeanings, setNewMeanings] = useState('');
  const [newExample, setNewExample] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [createMode, setCreateMode] = useState<'deck' | 'card'>('deck');

  const load = useCallback(async () => {
    const requestId = ++loadRequestId.current;
    if (!target) {
      setDecks([]);
      setSelectedDeck(null);
      setCards([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const [availableDecks, availableLanguages] = await Promise.all([decksApi.getDecks(), coursesApi.getLanguages()]);
      if (requestId !== loadRequestId.current) return;
      setDecks(availableDecks);
      setLanguages(availableLanguages);
      if (target?.languageId) setNewLanguageId(target.languageId);
      const allowed = sortUnits(availableDecks.filter((deck) => deckMatchesTarget(deck, target)));
      setSelectedDeck((current) => allowed.find((deck) => deck.id === current?.id) ?? allowed[0] ?? null);
    } catch (error) {
      if (requestId !== loadRequestId.current) return;
      message.error(getErrorMessage(error, t('common.error')));
      setDecks([]);
    } finally {
      if (requestId === loadRequestId.current) setLoading(false);
    }
  }, [target, t]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { setFilterCode(target?.languageCode ?? 'all'); }, [target?.languageCode]);
  useEffect(() => {
    setNewLanguageId(target?.languageId ?? '');
    setSelectedDeck((current) => current && deckMatchesTarget(current, target)
      ? current
      : decks.find((deck) => deckMatchesTarget(deck, target)) ?? null);
  }, [target, decks]);
  useEffect(() => {
    let active = true;
    const deckId = selectedDeck?.id;
    if (!deckId) {
      setCards([]);
      return;
    }
    decksApi.getCardsByDeckId(deckId)
      .then((items) => { if (active) setCards(items); })
      .catch((error) => { if (active) { setCards([]); message.error(getErrorMessage(error, t('common.error'))); } });
    return () => { active = false; };
  }, [selectedDeck?.id]);

  const filtered = useMemo(() => sortUnits(decks.filter((deck) => deckMatchesTarget(deck, target) && (
    filterCode === 'all' || deck.languageCode?.toLowerCase() === filterCode.toLowerCase()
  ))), [decks, filterCode, target]);
  const canEdit = selectedDeck?.ownerId === user?.id;

  const createDeck = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!newTitle.trim() || !target || newLanguageId !== target.languageId) return;
    try {
      const created = await decksApi.createDeck({
        title: newTitle.trim(), description: newDescription.trim(), languageId: newLanguageId,
        isPublic: newIsPublic, tags: [`course-level:${target.level}`]
      });
      setNewTitle('');
      setNewDescription('');
      setCreateOpen(false);
      await load();
      setSelectedDeck({ ...created, visibility: created.isPublic ? 'Public' : 'Private' });
      message.success(t('decks.created'));
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  const createCard = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedDeck || !newFront.trim() || !newBack.trim()) return;
    try {
      const card = await decksApi.createCard(selectedDeck.id, {
        front: newFront.trim(), back: newBack.trim(),
        phonetic: newPhonetic.trim(), partOfSpeech: newPartOfSpeech.trim(),
        meanings: newMeanings.split('\n').map((value) => value.trim()).filter(Boolean),
        example: newExample.trim()
      });
      setCards((items) => [...items, card]);
      setNewFront('');
      setNewBack('');
      setNewPhonetic('');
      setNewPartOfSpeech('');
      setNewMeanings('');
      setNewExample('');
      setCreateOpen(false);
      await load();
      message.success(t('decks.cardAdded'));
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  const deleteDeck = (deck: Deck) => Modal.confirm({
    title: isVi ? `Xóa bộ thẻ “${deck.title}”?` : `Delete “${deck.title}”?`,
    okType: 'danger',
    onOk: async () => {
      try {
        await decksApi.deleteDeck(deck.id);
        await load();
        message.success(t('decks.deleted'));
      } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
    }
  });

  const exportDeck = async (deck: Deck) => {
    try {
      const deckCards = await decksApi.getCardsByDeckId(deck.id);
      const payload = JSON.stringify({ deck, cards: deckCards }, null, 2);
      const url = URL.createObjectURL(new Blob([payload], { type: 'application/json' }));
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `deck_${deck.id}.json`;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  const importDeck = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    try {
      const data: unknown = JSON.parse(await file.text());
      if (!data || typeof data !== 'object' || !('deck' in data) || !('cards' in data)) throw new Error('Invalid deck JSON.');
      const bundle = data as { deck: Partial<Deck> & { languageCode?: string }; cards: Array<{ front: string; back: string }> };
      const languageId = languages.find((item) => item.code === bundle.deck.languageCode)?.id ?? bundle.deck.languageId;
      if (!target || languageId !== target.languageId || !bundle.deck.title || !Array.isArray(bundle.cards)) throw new Error('Invalid language, title or cards.');
      if (!deckMatchesTarget({ ...bundle.deck, languageId, tags: bundle.deck.tags ?? [] } as Deck, target)) throw new Error('Deck level does not match your study level.');
      const created = await decksApi.createDeck({
        title: bundle.deck.title, description: bundle.deck.description ?? '', languageId,
        isPublic: false, tags: [...(bundle.deck.tags ?? []).filter((tag) => !/^course-level:/i.test(tag)), `course-level:${target.level}`]
      });
      if (bundle.cards.length) await decksApi.createCardsBatch(created.id, bundle.cards);
      await load();
      setSelectedDeck(created);
      message.success(t('decks.imported'));
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  if (!target) return <div className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</div>;

  return (
    <div className="space-y-5 max-w-6xl mx-auto">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-bold text-stone-900"><BookOutlined /> {isVi ? 'Bộ thẻ của tôi' : 'My decks'}</h1>
        <div className="flex items-center gap-2">
          <div className="w-44"><SearchableSelect value={filterCode} onChange={setFilterCode} options={[{ value: 'all', label: t('common.allLanguages') }, ...languages.filter((item) => item.id === target?.languageId).map((item) => ({ value: item.code, label: item.name }))]} label={t('common.language')} /></div>
          <label className="px-3 py-2 bg-white border border-stone-300 rounded-lg text-xs font-bold cursor-pointer"><UploadOutlined /> {isVi ? 'Nhập JSON' : 'Import JSON'}<input type="file" accept=".json" onChange={importDeck} className="hidden" /></label>
          <button type="button" onClick={() => { setCreateMode('deck'); setCreateOpen(true); }} className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white"><PlusOutlined /> {isVi ? 'Tạo mới' : 'Create'}</button>
        </div>
      </header>

      <Modal title={isVi ? 'Tạo bộ thẻ hoặc thêm thẻ' : 'Create deck or add card'} open={createOpen} onCancel={() => setCreateOpen(false)} footer={null} destroyOnHidden>
      <div className="mb-4 flex gap-2" role="tablist" aria-label={isVi ? 'Loại nội dung' : 'Content type'}>
        <button type="button" role="tab" aria-selected={createMode === 'deck'} onClick={() => setCreateMode('deck')} className={`rounded-lg border px-3 py-2 text-xs font-bold ${createMode === 'deck' ? 'bg-amber-100' : 'bg-white'}`}>{isVi ? 'Bộ thẻ mới' : 'New deck'}</button>
        <button type="button" role="tab" aria-selected={createMode === 'card'} disabled={!canEdit} onClick={() => setCreateMode('card')} className={`rounded-lg border px-3 py-2 text-xs font-bold disabled:opacity-50 ${createMode === 'card' ? 'bg-amber-100' : 'bg-white'}`}>{isVi ? 'Thẻ mới' : 'New card'}</button>
      </div>
      {createMode === 'deck' ?
      <form onSubmit={createDeck} className="flex flex-col gap-3 pt-2">
        <input required value={newTitle} onChange={(event) => setNewTitle(event.target.value)} placeholder={isVi ? 'Tên bộ thẻ' : 'Deck title'} className="flex-1 min-w-40 p-2 border rounded-lg text-xs" />
        <input value={newDescription} onChange={(event) => setNewDescription(event.target.value)} placeholder={isVi ? 'Mô tả' : 'Description'} className="flex-1 min-w-40 p-2 border rounded-lg text-xs" />
        <div className="min-w-40"><SearchableSelect value={newLanguageId} onChange={setNewLanguageId} options={languages.filter((item) => item.id === target?.languageId).map((item) => ({ value: item.id, label: item.name }))} label={t('common.language')} /></div>
        <label className="flex items-center gap-1 text-xs"><input type="checkbox" checked={newIsPublic} onChange={(event) => setNewIsPublic(event.target.checked)} />{isVi ? 'Công khai' : 'Public'}</label>
        <div className="flex justify-end gap-2"><button type="button" onClick={() => setCreateOpen(false)} className="rounded-lg border px-3 py-2 text-xs">{t('common.cancel')}</button><button type="submit" className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white"><PlusOutlined /> {isVi ? 'Tạo bộ thẻ' : 'Create deck'}</button></div>
      </form>
      : canEdit && selectedDeck && <form onSubmit={createCard} className="flex flex-col gap-3 pt-2">
        <p className="text-xs text-stone-600">{selectedDeck.title}</p>
        <input required value={newFront} onChange={(event) => setNewFront(event.target.value)} placeholder={isVi ? 'Mặt trước' : 'Front'} className="w-full rounded-lg border p-2 text-xs" />
        <input required value={newBack} onChange={(event) => setNewBack(event.target.value)} placeholder={isVi ? 'Mặt sau' : 'Back'} className="w-full rounded-lg border p-2 text-xs" />
        <input value={newPhonetic} onChange={(event) => setNewPhonetic(event.target.value)} placeholder={isVi ? 'Phiên âm' : 'Phonetic'} aria-label={isVi ? 'Phiên âm' : 'Phonetic'} className="w-full rounded-lg border p-2 text-xs" />
        <input value={newPartOfSpeech} onChange={(event) => setNewPartOfSpeech(event.target.value)} placeholder={isVi ? 'Từ loại' : 'Part of speech'} aria-label={isVi ? 'Từ loại' : 'Part of speech'} className="w-full rounded-lg border p-2 text-xs" />
        <textarea value={newMeanings} onChange={(event) => setNewMeanings(event.target.value)} placeholder={isVi ? 'Các nghĩa, mỗi dòng một nghĩa' : 'Meanings, one per line'} aria-label={isVi ? 'Các nghĩa' : 'Meanings'} className="w-full rounded-lg border p-2 text-xs" />
        <input value={newExample} onChange={(event) => setNewExample(event.target.value)} placeholder={isVi ? 'Ví dụ' : 'Example'} aria-label={isVi ? 'Ví dụ' : 'Example'} className="w-full rounded-lg border p-2 text-xs" />
        <div className="flex justify-end gap-2"><button type="button" onClick={() => setCreateOpen(false)} className="rounded-lg border px-3 py-2 text-xs">{t('common.cancel')}</button><button type="submit" className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white"><PlusOutlined /> {isVi ? 'Lưu thẻ' : 'Save card'}</button></div>
      </form>}
      </Modal>

      <div className="grid md:grid-cols-[280px_1fr] gap-4">
        <div className="bg-white border-notebook rounded-xl divide-y max-h-[65vh] overflow-y-auto">
          {loading && <p className="p-4 text-xs">{isVi ? 'Đang tải...' : 'Loading...'}</p>}
          {!loading && filtered.length === 0 && <p className="p-4 text-xs text-stone-500">{isVi ? 'Chưa có bộ thẻ.' : 'No decks yet.'}</p>}
          {filtered.map((deck) => <button type="button" key={deck.id} onClick={() => setSelectedDeck(deck)} className={`w-full text-left p-3 text-xs ${selectedDeck?.id === deck.id ? 'bg-amber-100 font-bold' : 'hover:bg-stone-50'}`}>
            <span className="block truncate">{deck.title}</span><span className="text-stone-500">{deck.totalCards} {isVi ? 'thẻ' : 'cards'}</span>
          </button>)}
        </div>
        <div className="bg-white border-notebook rounded-xl p-4 space-y-4">
          {!selectedDeck ? <p className="text-xs text-stone-500">{isVi ? 'Chọn một bộ thẻ.' : 'Select a deck.'}</p> : <>
            <div className="flex flex-wrap justify-between gap-2">
              <div><h2 className="font-bold">{selectedDeck.title}</h2><p className="text-xs text-stone-600">{selectedDeck.description}</p></div>
              <div className="flex gap-2">
                <button type="button" onClick={() => onStudyDeck(selectedDeck.id)} className="px-3 py-1.5 bg-amber-400 rounded-lg text-xs font-bold">{isVi ? 'Ôn SRS' : 'Study SRS'}</button>
                <button type="button" onClick={() => exportDeck(selectedDeck)} className="px-3 py-1.5 border rounded-lg text-xs"><DownloadOutlined /></button>
                {canEdit && <button type="button" onClick={() => onOpenCsvImport(selectedDeck.id)} className="px-3 py-1.5 border rounded-lg text-xs">CSV</button>}
                {canEdit && <button type="button" onClick={() => deleteDeck(selectedDeck)} className="px-3 py-1.5 border border-red-300 text-red-700 rounded-lg text-xs"><DeleteOutlined /></button>}
              </div>
            </div>
            <div className="divide-y max-h-[50vh] overflow-y-auto">{cards.map((card) => <div key={card.id} className="py-2 text-xs">
              <strong>{card.front}</strong> {card.phonetic && <span className="text-stone-500">{card.phonetic}</span>}
              {card.partOfSpeech && card.partOfSpeech !== 'vocabulary' && <span className="ml-2 text-stone-500">{card.partOfSpeech}</span>}
              <div className="text-stone-700">{card.meanings?.length ? card.meanings.join('; ') : card.back}</div>
              {card.example && <div className="italic text-stone-500">{card.example}</div>}
            </div>)}</div>
          </>}
        </div>
      </div>
    </div>
  );
}
