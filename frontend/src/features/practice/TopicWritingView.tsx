import { useEffect, useMemo, useRef, useState } from 'react';
import { App } from 'antd';
import { ArrowLeftOutlined, SaveOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { curriculumStore } from '../../services/curriculumStore';
import { writingApi, getErrorMessage } from '../../services/apiClient';
import { SearchableSelect } from '../../components/ui/SearchableSelect';
import { SkillProgressBar } from './SkillProgressBar';
import { useSkillProgress } from './useSkillProgress';
import { playPracticeConfirmation } from '../../services/practiceFeedback';

interface TopicWritingViewProps {
  target: StudyTarget | null;
  onBack: () => void;
}

export function TopicWritingView({ target, onBack }: TopicWritingViewProps) {
  const { t, language } = useTranslation();
  const { message: appMessage } = App.useApp();
  const [promptId, setPromptId] = useState('');
  const [text, setText] = useState('');
  const [saved, setSaved] = useState(false);
  const pendingSubmissions = useRef(new Map<string, string>());
  const [saving, setSaving] = useState(false);
  const [loadingDraft, setLoadingDraft] = useState(false);
  const prompts = useMemo(() => curriculumStore.getWritingForTarget(target).filter((item) =>
    item.title?.trim() && item.instruction?.trim()
  ), [target]);
  const progress = useSkillProgress('writing', prompts);
  const current = prompts.find((item) => `${item.courseId}:${item.id}` === promptId) ?? prompts[0];
  useEffect(() => { if (progress.loaded && !promptId && prompts.length) { const last = prompts.find((item) => item.id === progress.resumeId) ?? prompts[0]; setPromptId(`${last.courseId}:${last.id}`); } }, [progress.loaded, progress.resumeId, promptId, prompts]);
  useEffect(() => { if (progress.loaded && promptId && current) void progress.record(current).catch(() => undefined); }, [progress.loaded, promptId, current?.id]);
  const currentKey = current ? `${current.courseId}:${current.id}` : '';
  const wordCount = text.trim() ? text.trim().split(/\s+/).length : 0;

  useEffect(() => {
    if (!currentKey) return;
    let active = true;
    setLoadingDraft(true);
    setText('');
    setSaved(false);
    writingApi.getLatest(currentKey)
      .then((draft) => { if (active && draft) { setText(draft.text); setSaved(true); } })
      .catch((error) => { if (active) appMessage.error(getErrorMessage(error, t('common.error'))); })
      .finally(() => { if (active) setLoadingDraft(false); });
    return () => { active = false; };
  }, [currentKey, appMessage]);

  const save = async () => {
    if (!currentKey || !text.trim() || saving) return;
    const submissionKey = `${currentKey}:${text.trim()}`;
    const clientSubmissionId = pendingSubmissions.current.get(submissionKey) ?? crypto.randomUUID();
    pendingSubmissions.current.set(submissionKey, clientSubmissionId);
    setSaving(true);
    try {
      await writingApi.save(currentKey, text.trim(), clientSubmissionId);
      if (current) await progress.record(current, undefined, true);
      pendingSubmissions.current.delete(submissionKey);
      setSaved(true);
      playPracticeConfirmation();
      appMessage.success(t('topicWriting.saved'));
    } catch (error) {
      appMessage.error(getErrorMessage(error, t('common.error')));
    } finally {
      setSaving(false);
    }
  };

  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      <button type="button" onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {t('common.back')}</button>
      <div>
        <h1 className="text-xl font-bold">{t('topicWriting.headerTitle')}</h1>
        <p className="text-xs text-stone-600">{target?.languageName ?? ''} · {target?.level ?? ''} · {t('topicWriting.promptCount', { count: prompts.length })}</p>
      </div>
    </header>

    {!target && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</p>}
    {target && prompts.length === 0 && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('topicWriting.noPrompts')}</p>}
    {target && <SkillProgressBar completed={progress.completed} practiced={progress.practiced} pendingAssessment total={prompts.length} language={language} error={progress.error} hasResume={!!progress.resumeId} onResume={() => { const last = prompts.find((item) => item.id === progress.resumeId); if (last) setPromptId(`${last.courseId}:${last.id}`); }} />}
    {prompts.length > 0 && <>
      <div className="rounded-xl border-notebook bg-white p-4"><SearchableSelect value={currentKey} onChange={setPromptId} options={prompts.map((item) => ({ value: `${item.courseId}:${item.id}`, label: item.title }))} label={t('topicWriting.choosePrompt')} /></div>

      {current && <section className="space-y-5 rounded-xl border-notebook bg-white p-5">
        <div>
          <p className="text-xs font-bold uppercase text-stone-500">{current.level} · {current.domain}</p>
          <h2 className="mt-1 text-lg font-bold">{current.title}</h2>
          {current.scenario && <p className="mt-2 text-sm text-stone-600">{current.scenario}</p>}
        </div>
        <div className="rounded-lg bg-amber-50 p-4 text-sm">
          <h3 className="font-bold">{t('topicWriting.taskInstruction')}</h3>
          <p className="mt-1">{current.instruction}</p>
          <p className="mt-2 text-xs text-stone-600">{t('topicWriting.targetWords', { count: current.minWords ?? 40 })}</p>
        </div>
        {(current.suggestedVocabulary?.length ?? 0) > 0 && <div>
          <h3 className="mb-2 text-sm font-bold">{t('topicWriting.suggestedKeywords')}</h3>
          <div className="flex flex-wrap gap-2">{current.suggestedVocabulary?.map((word) => <button key={word} type="button" onClick={() => { setText((value) => value ? `${value} ${word}` : word); setSaved(false); }} className="rounded-full border border-stone-300 px-3 py-1 text-xs">{word}</button>)}</div>
        </div>}
        <textarea value={text} onChange={(event) => { setText(event.target.value); setSaved(false); }} disabled={loadingDraft} placeholder={t('topicWriting.editorPlaceholder')} rows={12} className="w-full rounded-lg border border-stone-300 p-3 text-sm disabled:opacity-50" />
        <div className="flex flex-wrap items-center justify-between gap-3 text-xs">
          <p>{t('topicWriting.words')}: {wordCount} · {t('topicWriting.chars')}: {text.length}</p>
          <div className="flex items-center gap-3">
            {saved && <span role="status" className="text-emerald-700">{t('topicWriting.saved')}</span>}
            <button type="button" onClick={() => { void save(); }} disabled={!text.trim() || saving || loadingDraft} className="rounded-lg bg-stone-900 px-4 py-2 font-bold text-white disabled:opacity-50"><SaveOutlined /> {t('topicWriting.saveDraft')}</button>
          </div>
        </div>
      </section>}
    </>}
  </div>;
}
