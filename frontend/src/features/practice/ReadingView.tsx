import { useEffect, useMemo, useState } from 'react';
import { ArrowLeftOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { curriculumStore } from '../../services/curriculumStore';
import { speechService } from '../../services/speechService';
import { SearchableSelect } from '../../components/ui/SearchableSelect';
import { SkillProgressBar } from './SkillProgressBar';
import { useSkillProgress } from './useSkillProgress';
import { App } from 'antd';
import { getErrorMessage } from '../../services/apiClient';
import { playPracticeFeedback } from '../../services/practiceFeedback';

interface ReadingViewProps {
  target: StudyTarget | null;
  onBack: () => void;
}

export function ReadingView({ target, onBack }: ReadingViewProps) {
  const { t, language } = useTranslation();
  const { message } = App.useApp();
  const [articleId, setArticleId] = useState('');
  const [answer, setAnswer] = useState<number | null>(null);
  const [checked, setChecked] = useState(false);
  const [isCorrect, setIsCorrect] = useState(false);
  const articles = useMemo(() => curriculumStore.getReadingForTarget(target).filter((item) =>
    item.title?.trim() && item.content?.some((paragraph) => paragraph.trim()) && item.quiz?.question?.trim()
  ), [target]);
  const progress = useSkillProgress('reading', articles);
  const current = articles.find((item) => item.id === articleId) ?? articles[0];
  useEffect(() => { if (progress.loaded && !articleId && articles.length) setArticleId(progress.resumeId || articles[0].id); }, [progress.loaded, progress.resumeId, articleId, articles]);
  useEffect(() => { if (progress.loaded && articleId && current) void progress.record(current).catch(() => undefined); }, [progress.loaded, articleId, current?.id]);

  const checkAnswer = async () => {
    if (!current || answer === null) return;
    try {
      const result = await progress.record(current, answer);
      setIsCorrect(result.correct === true);
      setChecked(true);
      playPracticeFeedback(result.correct === true);
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  const chooseArticle = (id: string) => {
    setArticleId(id);
    setAnswer(null);
    setChecked(false);
  };

  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      <button type="button" onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {t('common.back')}</button>
      <div>
        <h1 className="text-xl font-bold">{t('reading.title')}</h1>
        <p className="text-xs text-stone-600">{target?.languageName ?? ''} · {target?.level ?? ''} · {t('reading.articleCount', { count: articles.length })}</p>
      </div>
    </header>

    {!target && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</p>}
    {target && articles.length === 0 && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('reading.noArticles')}</p>}
    {target && <SkillProgressBar completed={progress.completed} total={articles.length} language={language} error={progress.error} hasResume={!!progress.resumeId} onResume={() => chooseArticle(progress.resumeId)} />}
    {articles.length > 0 && <>
      <div className="rounded-xl border-notebook bg-white p-4"><SearchableSelect value={current?.id ?? ''} onChange={chooseArticle} options={articles.map((item) => ({ value: item.id, label: item.title }))} label={t('reading.chooseArticle')} /></div>

      {current && <article className="space-y-6 rounded-xl border-notebook bg-white p-5">
        <div>
          <p className="text-xs font-bold uppercase text-stone-500">{current.level} · {current.domain}</p>
          <h2 className="mt-1 text-lg font-bold">{current.title}</h2>
        </div>
        <div className="space-y-3 text-sm leading-7">{current.content.map((paragraph, index) => <p key={`${current.id}-${index}`}>{paragraph}</p>)}</div>
        {current.vocabularies?.length > 0 && <section className="space-y-2 border-t border-stone-200 pt-4">
          <h3 className="font-bold">{t('reading.keyVocabulary')}</h3>
          <div className="grid gap-2 sm:grid-cols-2">{current.vocabularies.map((item, index) => <div key={`${item.word}-${index}`} className="flex items-center justify-between gap-2 rounded-lg bg-stone-50 p-2 text-xs">
            <span><strong>{item.word}</strong> {item.ipa ?? ''} · {item.meaning}</span>
            <button type="button" aria-label={`${t('srs.listen')} ${item.word}`} onClick={() => speechService.speak(item.word, current.languageCode)}><SoundOutlined /></button>
          </div>)}</div>
        </section>}
        {current.quiz?.options?.length > 0 && <section className="space-y-3 border-t border-stone-200 pt-4">
          <h3 className="font-bold">{t('reading.question')}</h3>
          <p className="text-sm">{current.quiz.question}</p>
          <div className="space-y-2">{current.quiz.options.map((option, index) => <label key={`${current.id}-${index}`} className="flex cursor-pointer items-center gap-2 rounded-lg border border-stone-200 p-2 text-sm">
            <input type="radio" name={`reading-${current.id}`} checked={answer === index} onChange={() => { setAnswer(index); setChecked(false); }} />{option}
          </label>)}</div>
          <button type="button" disabled={answer === null} onClick={() => void checkAnswer()} className="rounded-lg bg-stone-900 px-4 py-2 text-xs font-bold text-white disabled:opacity-50">{t('reading.checkAnswer')}</button>
          {checked && <p role="status" className={`rounded-lg p-3 font-bold motion-safe:animate-pulse ${isCorrect ? 'bg-emerald-50 text-emerald-700' : 'bg-rose-50 text-rose-700'}`}>{t(isCorrect ? 'reading.correct' : 'reading.incorrect')}</p>}
          {checked && current.quiz.explanation && <p className="text-xs text-stone-600">{current.quiz.explanation}</p>}
        </section>}
      </article>}
    </>}
  </div>;
}
