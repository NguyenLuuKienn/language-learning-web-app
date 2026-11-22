import React, { useEffect, useState } from 'react';
import { App, Modal } from 'antd';
import { BookOutlined, PlusOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import { coursesApi, decksApi, dictionaryApi } from '../../services/apiClient';
import { speechService } from '../../services/speechService';
import type { Language, StudyTarget, Vocabulary } from '../../types';
import { SearchableSelect } from '../ui/SearchableSelect';

interface DictionaryEntry extends Vocabulary { languageId: string }

interface QuickDictionaryModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialWord?: string;
  target?: StudyTarget | null;
}

export const QuickDictionaryModal: React.FC<QuickDictionaryModalProps> = ({ isOpen, onClose, initialWord = '', target }) => {
  const { message } = App.useApp();
  const { t } = useTranslation();
  const [languages, setLanguages] = useState<Language[]>([]);
  const [languageId, setLanguageId] = useState(target?.languageId || '');
  const [term, setTerm] = useState(initialWord);
  const [results, setResults] = useState<DictionaryEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [savingId, setSavingId] = useState<string | null>(null);
  const [savedIds, setSavedIds] = useState<Set<string>>(() => new Set());
  const groupedResults = Array.from(
    results.reduce((groups, entry) => {
      const key = `${entry.languageId}:${entry.term.trim().toLocaleLowerCase()}`;
      const meanings = entry.meanings?.length ? entry.meanings : [entry.meaning];
      const existing = groups.get(key);
      if (existing) {
        for (const meaning of meanings) {
          if (!existing.meanings.some((value) => value.toLocaleLowerCase() === meaning.toLocaleLowerCase()))
            existing.meanings.push(meaning);
        }
        if (entry.example && !existing.examples.includes(entry.example)) existing.examples.push(entry.example);
        existing.ids.push(entry.id);
        if (!existing.phonetic && entry.phonetic) existing.phonetic = entry.phonetic;
        if (!existing.partOfSpeech && entry.partOfSpeech !== 'vocabulary') existing.partOfSpeech = entry.partOfSpeech;
      } else {
        groups.set(key, { ...entry, meanings: [...meanings], examples: entry.example ? [entry.example] : [], ids: [entry.id] });
      }
      return groups;
    }, new Map<string, DictionaryEntry & { meanings: string[]; examples: string[]; ids: string[] }>()).values()
  );

  useEffect(() => {
    if (!isOpen) return;
    coursesApi.getLanguages().then(setLanguages).catch((error) => message.error(String(error)));
  }, [isOpen, message]);

  useEffect(() => { setLanguageId(target?.languageId || ''); }, [target?.languageId]);
  useEffect(() => { setTerm(initialWord); }, [initialWord, isOpen]);

  useEffect(() => {
    if (!isOpen || !term.trim()) { setResults([]); return; }
    let active = true;
    const timer = setTimeout(() => {
      setLoading(true);
      dictionaryApi.search(term.trim(), languageId || undefined)
        .then((entries) => { if (active) setResults(entries); })
        .catch((error) => { if (active) { setResults([]); message.error(String(error)); } })
        .finally(() => { if (active) setLoading(false); });
    }, 250);
    return () => { active = false; clearTimeout(timer); };
  }, [isOpen, term, languageId, message]);

  const saveToSrs = async (entry: DictionaryEntry & { meanings: string[]; examples: string[]; ids: string[] }) => {
    setSavingId(entry.id);
    try {
      const decks = await decksApi.getDecks();
      let deck = decks.find((item) =>
        item.languageId === entry.languageId &&
        item.tags?.includes('dictionary') &&
        (!target || item.tags?.includes(`course-level:${target.level}`))
      );
      if (!deck) {
        deck = await decksApi.createDeck({ title: t('dictionary.savedDeckTitle'), description: t('dictionary.savedDeckDescription'), languageId: entry.languageId, isPublic: false, tags: ['dictionary'] });
      }
      const existingCards = await decksApi.getCardsByDeckId(deck.id);
      if (!existingCards.some((card) => card.front.trim().toLocaleLowerCase() === entry.term.trim().toLocaleLowerCase())) {
        await decksApi.createCard(deck.id, {
          front: entry.term,
          back: entry.meanings.join('; '),
          phonetic: entry.phonetic,
          partOfSpeech: entry.partOfSpeech,
          meanings: entry.meanings,
          example: entry.examples[0] || '',
          tags: ['dictionary']
        });
      }
      setSavedIds((current) => new Set([...current, ...entry.ids]));
      message.success(t('dictionary.saved'));
    } catch (error) {
      message.error(String(error));
    } finally {
      setSavingId(null);
    }
  };

  return <Modal open={isOpen} onCancel={onClose} footer={null} centered width={520} title={<span><BookOutlined /> {t('dictionary.title')}</span>}>
    <div className="space-y-4 pt-3">
      {target ? (
        <div className="rounded-lg border border-stone-300 bg-stone-50 p-3 text-sm" aria-label={t('dictionary.language')}>
          {languages.find((item) => item.id === target.languageId)?.name || t('dictionary.language')}
        </div>
      ) : (
        <SearchableSelect value={languageId} onChange={setLanguageId} options={[{ value: '', label: t('common.allLanguages') }, ...languages.map((item) => ({ value: item.id, label: item.name }))]} label={t('dictionary.language')} />
      )}
      <input value={term} onChange={(event) => setTerm(event.target.value)} placeholder={t('dictionary.searchPlaceholder')} className="w-full rounded-lg border border-stone-300 p-3 text-sm" aria-label={t('dictionary.searchPlaceholder')} />
      {loading && <p className="text-xs text-stone-500">{t('common.loading')}</p>}
      {!loading && term.trim() && results.length === 0 && <p className="text-xs text-stone-500">{t('dictionary.noResults')}</p>}
      <div className="max-h-80 space-y-3 overflow-y-auto">
        {groupedResults.map((entry) => <article key={entry.id} className="rounded-xl border border-stone-200 bg-amber-50/50 p-3 space-y-2">
          <div className="flex items-start justify-between gap-3">
            <div>
              <strong className="text-sm">{entry.term}</strong> <span className="text-xs text-stone-500">{entry.phonetic}</span>
              {entry.partOfSpeech && entry.partOfSpeech !== 'vocabulary' && <p className="text-xs text-stone-500">{entry.partOfSpeech}</p>}
              <ol className="list-decimal pl-5 text-xs">{entry.meanings.map((meaning, index) => <li key={index}>{meaning}</li>)}</ol>
            </div>
            <button type="button" onClick={() => speechService.speak(entry.term, languages.find((item) => item.id === entry.languageId)?.code || 'en')} aria-label={t('dictionary.listen')}><SoundOutlined /></button>
          </div>
          {entry.examples.map((example, index) => <p key={index} className="text-xs italic text-stone-600">{example}</p>)}
          <button type="button" disabled={savingId === entry.id || savedIds.has(entry.id)} onClick={() => saveToSrs(entry)} className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white disabled:opacity-50"><PlusOutlined /> {savedIds.has(entry.id) ? t('dictionary.saved') : t('dictionary.saveToSrs')}</button>
        </article>)}
      </div>
    </div>
  </Modal>;
};
