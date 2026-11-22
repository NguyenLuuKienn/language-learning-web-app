import { useEffect, useMemo, useState } from 'react';
import { ArrowLeftOutlined, PauseCircleOutlined, PlayCircleOutlined } from '@ant-design/icons';
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

interface ListeningViewProps { target: StudyTarget | null; onBack: () => void }

function dialogueLines(script: string) {
  return script.split(/\n|(?=\b[A-Z][\w .'-]{0,20}:\s)/g).map((line) => line.trim()).filter(Boolean);
}

export function ListeningView({ target, onBack }: ListeningViewProps) {
  const { t, language } = useTranslation();
  const { message } = App.useApp();
  const [selectedId, setSelectedId] = useState('');
  const [speed, setSpeed] = useState(1);
  const [playing, setPlaying] = useState(false);
  const [showTranscript, setShowTranscript] = useState(false);
  const [answer, setAnswer] = useState<number | null>(null);
  const [checked, setChecked] = useState(false);
  const [isCorrect, setIsCorrect] = useState(false);
  const exercises = useMemo(() => curriculumStore.getListeningForTarget(target).filter((item) => item.audioScript?.trim() && item.question?.trim()), [target]);
  const progress = useSkillProgress('listening', exercises);
  const current = exercises.find((item) => item.id === selectedId) ?? exercises[0];
  useEffect(() => { if (progress.loaded && !selectedId && exercises.length) setSelectedId(progress.resumeId || exercises[0].id); }, [progress.loaded, progress.resumeId, selectedId, exercises]);
  useEffect(() => { if (progress.loaded && selectedId && current) void progress.record(current).catch(() => undefined); }, [progress.loaded, selectedId, current?.id]);
  useEffect(() => () => speechService.stop(), []);

  const selectExercise = (id: string) => {
    speechService.stop();
    setPlaying(false);
    setSelectedId(id);
    setAnswer(null);
    setChecked(false);
    setShowTranscript(false);
  };

  const toggleAudio = () => {
    if (!current) return;
    if (playing) { speechService.stop(); setPlaying(false); return; }
    speechService.speak(current.audioScript, current.languageCode, {
      rate: speed, onStart: () => setPlaying(true), onEnd: () => setPlaying(false), onError: () => setPlaying(false)
    });
  };
  const checkAnswer = async () => {
    if (!current || answer === null) return;
    try {
      const result = await progress.record(current, answer);
      setIsCorrect(result.correct === true);
      setChecked(true);
      playPracticeFeedback(result.correct === true);
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  return <div className="mx-auto max-w-4xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      <button type="button" onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {t('common.back')}</button>
      <div><h1 className="text-xl font-bold">{t('listening.title')}</h1><p className="text-xs text-stone-600">{target?.languageName ?? ''} · {target?.level ?? ''}</p></div>
    </header>
    {!target && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</p>}
    {target && exercises.length === 0 && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('listening.noExercises')}</p>}
    {target && <SkillProgressBar completed={progress.completed} total={exercises.length} language={language} error={progress.error} hasResume={!!progress.resumeId} onResume={() => selectExercise(progress.resumeId)} />}
    {current && <section className="space-y-5 rounded-xl border-notebook bg-white p-5">
      <SearchableSelect value={current.id} onChange={selectExercise} options={exercises.map((item) => ({ value: item.id, label: item.title }))} label={t('listening.title')} />
      <p className="text-xs text-stone-600">{t('listening.fromCourse')}: {current.courseTitle} · {current.level}</p>
      <div className="flex flex-wrap items-center gap-3">
        <button type="button" onClick={toggleAudio} className="rounded-lg bg-stone-900 px-4 py-2 text-sm font-bold text-white">{playing ? <PauseCircleOutlined /> : <PlayCircleOutlined />} {t(playing ? 'listening.pause' : 'listening.playSample')}</button>
        <label className="text-sm">{t('listening.speed')} <select value={speed} onChange={(event) => setSpeed(Number(event.target.value))} className="ml-2 rounded border border-stone-300 bg-white p-1">{[0.8, 1, 1.2].map((value) => <option key={value} value={value}>{value}×</option>)}</select></label>
        <button type="button" onClick={() => setShowTranscript((value) => !value)} className="rounded-lg border border-stone-300 px-3 py-2 text-sm">{t(showTranscript ? 'listening.hideTranscript' : 'listening.showTranscript')}</button>
      </div>
      {showTranscript && <div className="space-y-2 rounded-lg bg-stone-50 p-4 text-sm leading-7">{dialogueLines(current.audioScript).map((line, index) => {
        const speaker = line.match(/^([^:]{1,24}):\s*(.*)$/);
        return <p key={`${current.id}-${index}`}>{speaker ? <><strong className="mr-2 text-stone-900">{speaker[1]}:</strong>{speaker[2]}</> : line}</p>;
      })}</div>}
      <div className="space-y-3 border-t border-stone-200 pt-4"><h2 className="font-bold">{current.question}</h2>
        {current.options.map((option, index) => <label key={`${current.id}-${index}`} className="flex cursor-pointer items-center gap-2 rounded-lg border border-stone-200 p-2 text-sm"><input type="radio" name={`listening-${current.id}`} checked={answer === index} onChange={() => { setAnswer(index); setChecked(false); }} />{option}</label>)}
        <button type="button" disabled={answer === null} onClick={() => void checkAnswer()} className="rounded-lg bg-stone-900 px-4 py-2 text-xs font-bold text-white disabled:opacity-50">{t('listening.checkAnswer')}</button>
        {checked && <p role="status" className={`rounded-lg p-3 font-bold motion-safe:animate-pulse ${isCorrect ? 'bg-emerald-50 text-emerald-700' : 'bg-rose-50 text-rose-700'}`}>{t(isCorrect ? 'listening.correct' : 'listening.incorrect')}</p>}
        {checked && current.explanation && <p className="text-sm text-stone-600">{current.explanation}</p>}
      </div>
    </section>}
  </div>;
}
