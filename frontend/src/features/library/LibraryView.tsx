import { useMemo, useState } from 'react';
import { ArrowLeftOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { curriculumStore } from '../../services/curriculumStore';
import { speechService } from '../../services/speechService';
import { Pagination } from '../../components/ui/Pagination';

interface LibraryViewProps {
  target?: StudyTarget | null;
  onBack?: () => void;
}

export function LibraryView({ target, onBack }: LibraryViewProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [page, setPage] = useState(1);
  const articles = useMemo(() => curriculumStore.getReadingForTarget(target ?? null).filter((item) =>
    item.title?.trim() && item.content?.some((paragraph) => paragraph.trim())
  ), [target]);
  const matches = articles.filter((item) => item.title.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()));
  const pageCount = Math.max(1, Math.ceil(matches.length / 8));
  const visibleArticles = matches.slice((Math.min(page, pageCount) - 1) * 8, Math.min(page, pageCount) * 8);
  const selected = articles.find((item) => item.id === selectedId);

  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      {onBack && <button type="button" onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {t('common.back')}</button>}
      <div>
        <h1 className="text-xl font-bold">{t('libraryPage.title')}</h1>
        <p className="text-xs text-stone-600">{target?.languageName ?? ''} · {target?.level ?? ''} · {t('reading.articleCount', { count: articles.length })}</p>
      </div>
    </header>

    {!target && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</p>}
    {target && articles.length === 0 && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('reading.noArticles')}</p>}

    {selected ? <article className="space-y-5 rounded-xl border-notebook bg-white p-5">
      <button type="button" onClick={() => setSelectedId('')} className="rounded-lg border border-stone-300 px-3 py-2 text-xs">{t('libraryPage.back')}</button>
      <div>
        <p className="text-xs font-bold uppercase text-stone-500">{selected.level} · {selected.domain}</p>
        <h2 className="mt-1 text-xl font-bold">{selected.title}</h2>
      </div>
      <div className="space-y-4">{selected.content.map((paragraph, index) => <div key={`${selected.id}-${index}`} className="flex gap-2 text-sm leading-7">
        <p className="flex-1">{paragraph}</p>
        <button type="button" onClick={() => speechService.speak(paragraph, selected.languageCode)} aria-label={t('libraryPage.listenParagraph')} className="self-start rounded-lg border border-stone-300 px-2 py-1"><SoundOutlined /></button>
      </div>)}</div>
    </article> : articles.length > 0 && <>
      <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder={t('libraryPage.searchPlaceholder')} aria-label={t('libraryPage.searchPlaceholder')} className="w-full rounded-xl border-notebook bg-white px-3 py-2 text-sm" />
      <div className="grid gap-4 md:grid-cols-2">{visibleArticles.map((item) => <article key={item.id} className="flex flex-col justify-between gap-4 rounded-xl border-notebook bg-white p-5">
        <div>
          <p className="text-xs font-bold text-emerald-700">{item.level} · {item.domain}</p>
          <h2 className="mt-2 font-bold">{item.title}</h2>
        </div>
        <button type="button" onClick={() => setSelectedId(item.id)} className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white">{t('libraryPage.readListen')}</button>
      </article>)}</div>
      <Pagination page={Math.min(page, pageCount)} pageCount={pageCount} onChange={setPage} />
      {matches.length === 0 && <p className="rounded-xl border-notebook bg-white p-5 text-sm">{t('reading.noMatch')}</p>}
    </>}
  </div>;
}
