import { useMemo, useState } from 'react';
import { ArrowLeftOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { curriculumStore } from '../../services/curriculumStore';
import { speechService } from '../../services/speechService';
import { SearchableSelect } from '../../components/ui/SearchableSelect';

interface AiConversationViewProps {
  target: StudyTarget | null;
  onBack: () => void;
}

export function AiConversationView({ target, onBack }: AiConversationViewProps) {
  const { t } = useTranslation();
  const [selectedId, setSelectedId] = useState('');
  const phrases = useMemo(() => curriculumStore.getSpeakingForTarget(target), [target]);
  const current = phrases.find((item) => item.id === selectedId) ?? phrases[0];

  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      <button type="button" onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {t('common.back')}</button>
      <div>
        <h1 className="text-xl font-bold">{t('aiConversation.badge')}</h1>
        <p className="text-xs text-stone-600">{target?.languageName ?? ''} · {target?.level ?? ''}</p>
      </div>
    </header>
    {!target && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</p>}
    {target && phrases.length === 0 && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('speaking.noPhrases')}</p>}
    {current && <section className="space-y-5 rounded-xl border-notebook bg-white p-5">
      <SearchableSelect value={current.id} onChange={setSelectedId} options={phrases.map((item) => ({ value: item.id, label: item.title }))} label={t('aiConversation.scenarios')} />
      <div className="rounded-lg bg-amber-50 p-5">
        {current.context && <p className="mb-2 text-sm text-stone-600">{current.context}</p>}
        <p className="text-xl font-bold">{current.phrase}</p>
        {current.phonetic && <p className="mt-2 text-sm text-stone-500">{current.phonetic}</p>}
        <p className="mt-2 text-sm text-stone-600">{current.translation}</p>
      </div>
      <button type="button" onClick={() => speechService.speak(current.phrase, current.languageCode)} className="rounded-lg bg-stone-900 px-4 py-2 text-sm font-bold text-white"><SoundOutlined /> {t('aiConversation.listen')}</button>
      <p className="text-sm text-stone-600">{t('aiConversation.tip')}</p>
    </section>}
  </div>;
}
