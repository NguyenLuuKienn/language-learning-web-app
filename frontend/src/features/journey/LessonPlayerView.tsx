import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { App } from 'antd';
import { ArrowLeftOutlined, ArrowRightOutlined, AudioOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import { ApiError, coursesApi, getErrorMessage, lessonJourneyApi, skillPracticeApi, writingApi, type LessonJourney, type LessonJourneyStep } from '../../services/apiClient';
import type { CurriculumCourse, CurriculumLesson } from '../../services/curriculumStore';
import { speechService } from '../../services/speechService';
import type { StudyTarget } from '../../types';
import { VocabularyLearningView } from '../reviews/VocabularyLearningView';
import { QuizPlayerView } from '../quiz/QuizPlayerView';

interface Props { courseId: string; lessonId: string; target: StudyTarget | null; requestedStep?: string | null }

type PracticeActivity = Record<string, unknown> & { id: string };
const sectionByType: Record<string, keyof CurriculumLesson> = {
  grammar: 'grammarTopics', listening: 'listeningExercises', speaking: 'speakingExercises',
  reading: 'readingArticles', writing: 'writingPrompts'
};
const labels: Record<string, string> = {
  introduction: 'Giới thiệu', vocabulary: 'Từ vựng', grammar: 'Ngữ pháp', listening: 'Nghe',
  speaking: 'Nói', reading: 'Đọc', writing: 'Viết', quiz: 'Kiểm tra', recap: 'Tổng kết'
};
const statusLabels: Record<string, string> = { Completed: 'Đã học', InProgress: 'Đang học', Available: 'Sẵn sàng', Locked: 'Chưa mở' };

function lines(value: string) {
  return value.replace(/\s+(?=[\p{L}][\p{L}\s()]{0,30}:\s)/gu, '\n').split(/\n+/).filter(Boolean);
}

interface BrowserRecognition {
  lang: string; continuous: boolean; interimResults: boolean;
  onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
  onerror: (() => void) | null; onend: (() => void) | null;
  start: () => void; stop: () => void;
}
const speechLocale = (code: string) => ({ ja: 'ja-JP', zh: 'zh-CN', ko: 'ko-KR', fr: 'fr-FR', de: 'de-DE', en: 'en-US' })[code.toLowerCase()] ?? 'en-US';

export const LessonPlayerView: React.FC<Props> = ({ courseId, lessonId, target, requestedStep }) => {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [journey, setJourney] = useState<LessonJourney | null>(null);
  const [material, setMaterial] = useState<CurriculumCourse | null>(null);
  const [selectedStepId, setSelectedStepId] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [checkpointConflict, setCheckpointConflict] = useState(false);
  const [answer, setAnswer] = useState<number | null>(null);
  const [feedback, setFeedback] = useState<'correct' | 'incorrect' | ''>('');
  const [writingText, setWritingText] = useState('');
  const [writingDirty, setWritingDirty] = useState(false);
  const [writingSaveFailed, setWritingSaveFailed] = useState(false);
  const writingQueue = useRef<Promise<unknown>>(Promise.resolve());
  const pendingSkillAttempts = useRef(new Map<string, string>());
  const [transcriptOpen, setTranscriptOpen] = useState(false);
  const [spokenTranscript, setSpokenTranscript] = useState('');
  const [recording, setRecording] = useState(false);
  const recognitionRef = useRef<BrowserRecognition | null>(null);
  const revisions = useRef<Record<string, number>>({});
  const checkpointQueue = useRef<Promise<unknown>>(Promise.resolve());
  const contentVersion = useRef('');

  const syncRevisions = (state: LessonJourney) => {
    for (const item of state.steps) revisions.current[`${state.lessonId}:${item.id}`] = item.revision;
  };
  const persistStep = (stepId: string, value: { cursor?: number; activityId?: string; draftJson?: string; completeIntroduction?: boolean }) => {
    const key = `${lessonId}:${stepId}`;
    const save = checkpointQueue.current.catch(() => undefined).then(async () => {
      const state = await lessonJourneyApi.visit(lessonId, stepId, { ...value, expectedRevision: revisions.current[key] ?? 0, contentVersion: contentVersion.current });
      syncRevisions(state);
      contentVersion.current = state.contentVersion;
      return state;
    });
    void save.catch(cause => { if (cause instanceof ApiError && cause.status === 409) setCheckpointConflict(true); });
    checkpointQueue.current = save;
    return save;
  };

  useEffect(() => () => recognitionRef.current?.stop(), []);

  const reload = useCallback(async () => {
    setError('');
    try {
      const [initial, raw] = await Promise.all([lessonJourneyApi.get(lessonId), coursesApi.getCourseMaterial(courseId)]);
      if (!raw || typeof raw !== 'object' || !('units' in raw)) throw new Error('Course material is unavailable.');
      if (initial.courseId.toLowerCase() !== courseId.toLowerCase() || !(raw as CurriculumCourse).units?.some(unit => unit.lessons?.some(item => item.id.toLowerCase() === lessonId.toLowerCase())))
        throw new Error(vi ? 'Bài học không thuộc khóa học này hoặc nội dung đã thay đổi.' : 'This lesson is not in the selected course or its content has changed.');
      syncRevisions(initial);
      contentVersion.current = initial.contentVersion;
      const state = initial.status === 'NotStarted' && initial.currentStepId
        ? await persistStep(initial.currentStepId, { cursor: 0 }) : initial;
      setJourney(state);
      setMaterial(raw as CurriculumCourse);
      setCheckpointConflict(false);
      setSelectedStepId(current => current && state.steps.some(step => step.id === current && step.status !== 'Locked')
        ? current : requestedStep && state.steps.some(step => step.id === requestedStep && step.status !== 'Locked')
          ? requestedStep : state.currentStepId);
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không tải được bài học.' : 'Could not load the lesson.')); }
  }, [courseId, lessonId, requestedStep, vi]);

  useEffect(() => { setSelectedStepId(''); setMaterial(null); setJourney(null); void reload(); }, [reload]);
  const lesson = useMemo(() => material?.units.flatMap(unit => unit.lessons).find(item => item.id === lessonId), [material, lessonId]);
  const step = journey?.steps.find(item => item.id === selectedStepId) ?? journey?.steps.find(item => item.id === journey.currentStepId);
  const section = step ? sectionByType[step.type] : undefined;
  const activities = useMemo(() => {
    const raw = section && lesson ? lesson[section] : [];
    return (Array.isArray(raw) ? raw : []).filter(item =>
      typeof item === 'object' && item !== null && 'id' in item && typeof item.id === 'string' &&
      !!step?.activityRefs.includes(item.id)).map(item => item as unknown as PracticeActivity);
  }, [lesson, section, step]);
  const currentActivity = activities[Math.min(step?.cursor ?? 0, Math.max(0, activities.length - 1))];
  const toggleSpeaking = () => {
    if (recording) { recognitionRef.current?.stop(); return; }
    const browser = window as typeof window & { SpeechRecognition?: new () => BrowserRecognition; webkitSpeechRecognition?: new () => BrowserRecognition };
    const Recognition = browser.SpeechRecognition ?? browser.webkitSpeechRecognition;
    if (!Recognition || !target) { message.info(vi ? 'Trình duyệt chưa hỗ trợ chép lời. Bạn vẫn có thể đọc thành tiếng và xác nhận đã luyện.' : 'Speech transcription is unavailable. You can speak aloud and confirm practice.'); return; }
    const recognition = new Recognition();
    recognition.lang = speechLocale(target.languageCode);
    recognition.continuous = false;
    recognition.interimResults = false;
    recognition.onresult = event => setSpokenTranscript(event.results[0]?.[0]?.transcript || '');
    recognition.onerror = () => { setRecording(false); message.error(vi ? 'Không nhận được giọng nói. Hãy thử lại.' : 'Could not capture speech. Please retry.'); };
    recognition.onend = () => setRecording(false);
    recognitionRef.current = recognition;
    setSpokenTranscript('');
    recognition.start(); setRecording(true);
  };

  useEffect(() => { recognitionRef.current?.stop(); setRecording(false); setSpokenTranscript(''); }, [step?.id, currentActivity?.id]);
  const saveWritingDraft = (text: string, selected: LessonJourneyStep, activityId: string) => {
    const save = writingQueue.current.catch(() => undefined).then(() => persistStep(selected.id,
      { cursor: selected.cursor, activityId, draftJson: JSON.stringify({ text }) }));
    writingQueue.current = save;
    return save;
  };

  const visit = async (selected: LessonJourneyStep, cursor = selected.cursor, extras: { activityId?: string; draftJson?: string; completeIntroduction?: boolean } = {}) => {
    setBusy(true);
    try {
      const state = await persistStep(selected.id, { cursor, activityId: extras.activityId, draftJson: extras.draftJson, completeIntroduction: extras.completeIntroduction });
      setJourney(state); setSelectedStepId(selected.id); setAnswer(null); setFeedback('');
    } catch (cause) { message.error(getErrorMessage(cause, vi ? 'Không lưu được tiến độ.' : 'Could not save progress.')); }
    finally { setBusy(false); }
  };
  const selectStep = async (selected: LessonJourneyStep) => {
    if (selected.status === 'Locked') return;
    if (step?.type === 'writing' && currentActivity) {
      try {
        if (writingDirty) await saveWritingDraft(writingText, step, currentActivity.id);
        else await writingQueue.current;
      } catch (cause) { message.error(getErrorMessage(cause, vi ? 'Bản nháp chưa được lưu.' : 'Draft was not saved.')); return; }
    }
    setSelectedStepId(selected.id); setAnswer(null); setFeedback('');
    void visit(selected);
  };
  const refresh = async (advance = false) => {
    try {
      await checkpointQueue.current.catch(() => undefined);
      const updated = await lessonJourneyApi.refresh(lessonId);
      if (contentVersion.current && updated.contentVersion !== contentVersion.current) {
        await reload();
        return;
      }
      syncRevisions(updated);
      contentVersion.current = updated.contentVersion;
      setJourney(updated);
      if (advance) setSelectedStepId(updated.currentStepId);
    } catch (cause) { message.error(getErrorMessage(cause, vi ? 'Không cập nhật được tiến độ.' : 'Could not refresh progress.')); }
  };
  const finishActivity = async () => {
    if (!step || !currentActivity || !section || !journey) return;
    setBusy(true);
    try {
      const skillKey = `${step.type}:${courseId}:${currentActivity.id}:${step.type === 'writing' ? writingText.trim() : answer ?? ''}`;
      const clientAttemptId = pendingSkillAttempts.current.get(skillKey) ?? crypto.randomUUID();
      pendingSkillAttempts.current.set(skillKey, clientAttemptId);
      if (step.type === 'writing') {
        if (!writingText.trim()) return;
        await saveWritingDraft(writingText, step, currentActivity.id);
        await writingApi.save(currentActivity.id, writingText.trim(), clientAttemptId);
        await skillPracticeApi.record('writing', courseId, currentActivity.id, undefined, true, clientAttemptId);
      } else if (step.type === 'speaking') {
        await skillPracticeApi.record('speaking', courseId, currentActivity.id, undefined, true, clientAttemptId);
      } else if (answer !== null && (step.type === 'grammar' || step.type === 'listening' || step.type === 'reading')) {
        const result = await skillPracticeApi.record(step.type, courseId, currentActivity.id, answer, false, clientAttemptId);
        pendingSkillAttempts.current.delete(skillKey);
        setFeedback(result.correct ? 'correct' : 'incorrect');
        if (!result.correct) return;
      } else return;
      pendingSkillAttempts.current.delete(skillKey);
      const nextCursor = Math.min(step.cursor + 1, activities.length - 1);
      const updated = await persistStep(step.id, { cursor: nextCursor,
        activityId: activities[nextCursor]?.id, ...(step.type === 'writing' ? { draftJson: '{}' } : {}) });
      setJourney(updated); setAnswer(null); setWritingText(''); setWritingDirty(false);
      if (updated.steps.find(item => item.id === step.id)?.status === 'Completed') setSelectedStepId(updated.currentStepId);
    } catch (cause) { message.error(getErrorMessage(cause, vi ? 'Không lưu được bài làm.' : 'Could not save your work.')); }
    finally { setBusy(false); }
  };

  useEffect(() => {
    if (step?.type !== 'writing' || !currentActivity) return;
    let active = true;
    setWritingSaveFailed(false);
    let draft: string | undefined;
    try { const parsed = JSON.parse(step.draftJson || '{}') as { text?: unknown }; if (typeof parsed.text === 'string') draft = parsed.text; } catch { /* Legacy draft. */ }
    if (draft !== undefined) { setWritingText(draft); setWritingDirty(false); }
    else writingApi.getLatest(currentActivity.id).then(saved => { if (active) { setWritingText(saved?.text || ''); setWritingDirty(false); } }).catch(() => {});
    return () => { active = false; };
  }, [step?.type, step?.id, currentActivity?.id]);

  useEffect(() => {
    if (!writingDirty || writingSaveFailed || step?.type !== 'writing' || !currentActivity) return;
    const timer = window.setTimeout(() => {
      setWritingDirty(false);
      void saveWritingDraft(writingText, step, currentActivity.id).catch(cause => {
        setWritingDirty(true);
        setWritingSaveFailed(true);
        message.error(getErrorMessage(cause, vi ? 'Không lưu được bản nháp.' : 'Could not save your draft.'));
      });
    }, 700);
    return () => window.clearTimeout(timer);
  }, [writingText, writingDirty, writingSaveFailed, step?.id, currentActivity?.id]);

  const saveAndExit = async () => {
    if (step?.type === 'writing' && currentActivity) {
      try {
        if (writingDirty) await saveWritingDraft(writingText, step, currentActivity.id);
        else await writingQueue.current;
      }
      catch (cause) { message.error(getErrorMessage(cause, vi ? 'Bản nháp chưa được lưu. Hãy thử lại.' : 'Draft was not saved. Please retry.')); return; }
    }
    navigate('/roadmap');
  };

  if (!target) return <div className="rounded-xl border-notebook bg-white p-6">{vi ? 'Hãy chọn mục tiêu học trước.' : 'Choose a learning goal first.'}</div>;
  if (error) return <div role="alert" className="mx-auto max-w-3xl rounded-xl border-notebook bg-white p-6"><p>{error}</p><button type="button" className="mt-3 rounded-lg bg-amber-300 px-4 py-2" onClick={() => void reload()}>{vi ? 'Thử lại' : 'Retry'}</button></div>;
  if (!journey || !material || !lesson || !step) return <div className="p-6">{vi ? 'Đang tải bài học...' : 'Loading lesson...'}</div>;
  const done = journey.status === 'Completed';
  const stepIndex = journey.steps.findIndex(item => item.id === step.id);
  const question = step.type === 'reading' ? (currentActivity?.quiz as Record<string, unknown> | undefined) : currentActivity;
  const choices = Array.isArray(question?.options) ? question.options as string[] : [];
  return <div className="mx-auto max-w-6xl space-y-5">
    <button type="button" onClick={() => void saveAndExit()} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {vi ? 'Lộ trình' : 'Roadmap'}</button>
    {journey.contentChangedSinceStart && <div role="alert" className="rounded-xl border border-amber-400 bg-amber-50 p-4 text-sm">
      {vi ? 'Nội dung bài học đã được cập nhật từ khi bạn bắt đầu. Tiến độ các bước cũ vẫn được giữ; hãy xem lại nội dung trước khi tiếp tục.' : 'This lesson changed since you started. Your previous progress is preserved; review the updated content before continuing.'}
    </div>}
    <header className="rounded-2xl border-notebook-2 bg-white p-5">
      <p className="text-xs text-stone-500">{journey.courseTitle} / {journey.unitTitle}</p>
      <h1 className="mt-2 text-2xl font-black">{journey.lessonTitle}</h1>
      {journey.summary && <p className="mt-2 text-sm text-stone-600">{journey.summary}</p>}
      <div className="mt-4 flex items-center justify-between text-xs font-bold"><span>{vi ? 'Tiến độ bài học' : 'Lesson progress'}</span><span>{journey.completedRequiredSteps}/{journey.totalRequiredSteps}</span></div>
      <div className="mt-2 h-2 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-emerald-600" style={{ width: `${journey.totalRequiredSteps ? journey.completedRequiredSteps / journey.totalRequiredSteps * 100 : 0}%` }} /></div>
    </header>
    {checkpointConflict && <div role="alert" className="rounded-xl border border-amber-400 bg-amber-50 p-4 text-sm"><strong>{vi ? 'Tiến độ đã thay đổi ở nơi khác.' : 'Progress changed elsewhere.'}</strong><p className="mt-1">{vi ? 'Bản nháp đang gõ vẫn ở đây. Hãy sao chép nếu cần, rồi tải tiến độ mới trước khi lưu tiếp.' : 'Your local draft is still visible. Copy it if needed, then load the latest progress before saving.'}</p><button type="button" onClick={() => void reload()} className="mt-2 rounded-lg bg-amber-300 px-3 py-2 font-bold">{vi ? 'Tải tiến độ mới' : 'Load latest progress'}</button></div>}
    {done && <div role="status" className="rounded-xl border border-emerald-300 bg-emerald-50 p-5"><strong>{vi ? 'Đã hoàn thành bài học' : 'Lesson complete'}</strong><p className="mt-1 text-sm">{vi ? 'Bạn có thể xem lại từng bước hoặc sang bài tiếp theo từ lộ trình.' : 'Review any step or continue from the roadmap.'}</p></div>}
    {(journey.weakPoints ?? []).length > 0 && <section className="rounded-xl border border-amber-300 bg-amber-50 p-4" aria-label={vi ? 'Gợi ý luyện lại' : 'Practice suggestions'}>
      <h2 className="text-sm font-bold">{vi ? 'Gợi ý luyện lại từ câu sai gần nhất' : 'Practice suggestions from recent mistakes'}</h2>
      <p className="mt-1 text-xs text-stone-600">{vi ? 'Câu đã làm đúng ở lần gần nhất sẽ không còn trong danh sách này.' : 'Items answered correctly on the latest attempt leave this list.'}</p>
      <div className="mt-3 flex flex-wrap gap-2">{journey.weakPoints.map(item => {
        const relatedStep = journey.steps.find(candidate => candidate.type === item.stepType);
        return <button key={`${item.stepType}:${item.activityId}`} type="button" disabled={!relatedStep || relatedStep.status === 'Locked'}
          onClick={() => { if (relatedStep) void selectStep(relatedStep); }}
          className="rounded-lg border border-amber-400 bg-white px-3 py-2 text-left text-xs disabled:opacity-50">
          <strong>{vi ? labels[item.stepType] || item.stepType : relatedStep?.title || item.stepType}</strong>
          <span className="ml-2">{item.prompt}</span>
          <span className="ml-2 text-amber-800">{item.incorrectAttempts}×</span>
        </button>;
      })}</div>
    </section>}
    <div className="grid gap-5 lg:grid-cols-[260px_minmax(0,1fr)]">
      <nav aria-label={vi ? 'Các bước bài học' : 'Lesson steps'} className="self-start rounded-2xl border-notebook bg-white p-3">
        <p className="mb-2 px-2 text-xs font-bold uppercase text-stone-500">{vi ? 'Hành trình bài học' : 'Lesson journey'}</p>
        <div className="space-y-1">{journey.steps.map((item, index) => <button key={item.id} type="button" disabled={item.status === 'Locked'} onClick={() => selectStep(item)}
          className={`flex w-full items-center justify-between gap-2 rounded-lg px-3 py-3 text-left text-sm ${item.id === step.id ? 'bg-amber-100 font-bold' : 'hover:bg-stone-50'} disabled:cursor-not-allowed disabled:opacity-50`}>
          <span>{index + 1}. {vi ? labels[item.type] || item.title : item.title}</span><span className="text-xs">{item.status === 'Completed' ? '✓' : item.status === 'Locked' ? '🔒' : '→'}</span>
        </button>)}</div>
      </nav>
      <main className="min-w-0 rounded-2xl border-notebook-2 bg-white p-5 sm:p-7">
        <div className="mb-5 flex items-center justify-between gap-3"><div><p className="text-xs text-amber-800">{vi ? `Bước ${stepIndex + 1}/${journey.steps.length}` : `Step ${stepIndex + 1}/${journey.steps.length}`}</p><h2 className="text-xl font-bold">{vi ? labels[step.type] || step.title : step.title}</h2></div><span className="rounded-full bg-stone-100 px-3 py-1 text-xs">{vi ? statusLabels[step.status] || step.status : step.status}</span></div>
        {step.type === 'introduction' && <div className="space-y-4"><p className="rounded-xl bg-[#FAF6EE] p-5 text-sm leading-7">{journey.summary || (vi ? `Bài học: ${journey.lessonTitle}. Xem các nội dung và hoạt động bên dưới để đạt mục tiêu của bài.` : `Lesson: ${journey.lessonTitle}. Work through the activities below.`)}</p><button type="button" disabled={busy} onClick={() => void visit(step, 0, { completeIntroduction: true }) .then(() => void refresh(true))} className="rounded-lg bg-amber-300 px-5 py-3 text-sm font-bold">{vi ? 'Đã hiểu mục tiêu · Tiếp tục' : 'Got it · Continue'}</button></div>}
        {step.type === 'vocabulary' && <VocabularyLearningView target={target} lessonId={lessonId} onBack={() => void refresh()} onCompleted={() => void refresh(true)} />}
        {step.type === 'quiz' && <QuizPlayerView lessonId={lessonId} lessonTitle={journey.lessonTitle} initialDraft={step.draftJson}
          onDraft={(draftJson, cursor) => { void persistStep(step.id, { cursor, draftJson,
            activityId: lesson.quizQuestions[cursor]?.id }).then(setJourney).catch(cause => message.error(getErrorMessage(cause, vi ? 'Không lưu được bài kiểm tra đang làm.' : 'Could not save quiz progress.'))); }}
          onBack={() => void refresh()} onResult={() => void refresh(true)} />}
        {step.type === 'recap' && <div><p className="mb-4 text-sm">{journey.summary || journey.lessonTitle}</p><button type="button" onClick={() => void visit(step, 0, { completeIntroduction: true })} className="rounded-lg bg-amber-300 px-4 py-2">{vi ? 'Hoàn tất tổng kết' : 'Finish recap'}</button></div>}
        {section && <div className="space-y-5">
          {activities.length === 0 && <div role="alert" className="rounded-xl border border-rose-300 bg-rose-50 p-4 text-sm">{vi ? 'Nội dung của bước này chưa có dữ liệu hợp lệ. Vui lòng thử lại sau.' : 'This step has no usable content. Please try again later.'}</div>}
          {currentActivity && <><p className="text-xs text-stone-500">{(step.cursor ?? 0) + 1}/{activities.length}</p>
            <h3 className="text-lg font-bold">{String(currentActivity.title || currentActivity.question || journey.lessonTitle)}</h3>
            {step.type === 'grammar' && <div className="space-y-2 rounded-xl bg-[#FAF6EE] p-4 text-sm"><p>{String(currentActivity.explanation || '')}</p><strong>{String(currentActivity.formula || '')}</strong><p>{String(currentActivity.example || '')}</p><p className="text-stone-500">{String(currentActivity.exampleTranslation || '')}</p></div>}
            {step.type === 'listening' && <div className="space-y-3"><button type="button" onClick={() => speechService.speak(String(currentActivity.audioScript || ''), target.languageCode)} className="rounded-lg border-notebook px-4 py-2 text-sm"><SoundOutlined /> {vi ? 'Nghe hội thoại' : 'Play dialogue'}</button><button type="button" onClick={() => setTranscriptOpen(value => !value)} className="ml-2 rounded-lg border-notebook px-4 py-2 text-sm">{transcriptOpen ? (vi ? 'Ẩn bản chép' : 'Hide transcript') : (vi ? 'Xem bản chép' : 'Transcript')}</button>{transcriptOpen && <div className="space-y-2 rounded-xl bg-[#FAF6EE] p-4 text-sm">{lines(String(currentActivity.audioScript || '')).map((line, index) => <p key={index}>{line}</p>)}</div>}</div>}
            {step.type === 'speaking' && <div className="space-y-3 rounded-xl bg-[#FAF6EE] p-4 text-sm"><p>{String(currentActivity.context || '')}</p><div className="space-y-2">{lines(String(currentActivity.phrase || '')).map((line, index) => <p key={index}>{line}</p>)}</div><p className="text-stone-600">{String(currentActivity.translation || '')}</p><div className="flex flex-wrap gap-2"><button type="button" onClick={() => speechService.speak(String(currentActivity.phrase || ''), target.languageCode)} className="rounded-lg border-notebook px-3 py-2"><SoundOutlined /> {vi ? 'Nghe mẫu' : 'Listen'}</button><button type="button" onClick={toggleSpeaking} className="rounded-lg border-notebook px-3 py-2"><AudioOutlined /> {recording ? (vi ? 'Dừng ghi âm' : 'Stop') : (vi ? 'Bắt đầu luyện nói' : 'Start speaking')}</button></div>{spokenTranscript && <div role="status" className="rounded-lg border border-emerald-200 bg-white p-3"><strong>{vi ? 'Trình duyệt nghe được:' : 'Transcript:'}</strong><p>{spokenTranscript}</p></div>}<p className="text-xs text-stone-500">{vi ? 'Bản chép lời chỉ giúp đối chiếu từ ngữ; đây chưa phải điểm phát âm. Sau khi đọc thành tiếng, hãy xác nhận đã luyện.' : 'The transcript helps compare words; it is not a pronunciation score. Confirm after speaking aloud.'}</p></div>}
            {step.type === 'reading' && <div className="space-y-3 rounded-xl bg-[#FAF6EE] p-4 text-sm">{(Array.isArray(currentActivity.content) ? currentActivity.content as string[] : []).map((paragraph, index) => <p key={index}>{paragraph}</p>)}</div>}
            {step.type === 'writing' && <div className="space-y-3"><p className="text-sm">{String(currentActivity.scenario || '')}</p><p className="rounded-xl bg-[#FAF6EE] p-4 text-sm">{String(currentActivity.instruction || '')}</p><textarea value={writingText} onChange={event => { setWritingText(event.target.value); setWritingDirty(true); setWritingSaveFailed(false); }} rows={9} className="w-full rounded-xl border-notebook p-3 text-sm" aria-label={vi ? 'Bài viết' : 'Writing answer'} /><p className="text-xs text-stone-500">{vi ? 'Bản nháp tự lưu; nộp bài mới ghi đã thực hành, chưa chấm chất lượng.' : 'Drafts autosave; submitting records practice, not a quality score.'}</p>{writingSaveFailed && <p role="alert" className="text-xs font-bold text-rose-700">{vi ? 'Chưa lưu được bản nháp. Hãy kiểm tra kết nối hoặc bấm Lưu và thoát để thử lại.' : 'Draft not saved. Check your connection or use Save and exit to retry.'}</p>}</div>}
            {choices.length > 0 && <div className="space-y-2"><p className="font-bold">{String(question?.question || '')}</p>{choices.map((choice, index) => <label key={index} className="flex cursor-pointer gap-3 rounded-lg border border-stone-200 p-3 text-sm"><input type="radio" name={`${step.id}-${currentActivity.id}`} checked={answer === index} onChange={() => { setAnswer(index); setFeedback(''); }} />{choice}</label>)}</div>}
            {feedback && <p role="status" className={`rounded-lg p-3 text-sm font-bold ${feedback === 'correct' ? 'bg-emerald-50 text-emerald-800' : 'bg-rose-50 text-rose-800'}`}>{feedback === 'correct' ? (vi ? 'Chính xác. Tiếp tục nhé.' : 'Correct. Keep going.') : (vi ? 'Chưa đúng. Đọc lại ngữ cảnh và thử lại.' : 'Not yet. Review the context and try again.')}</p>}
            <button type="button" disabled={busy || (choices.length > 0 && answer === null) || (step.type === 'writing' && !writingText.trim())} onClick={() => void finishActivity()} className="rounded-lg bg-amber-300 px-5 py-3 text-sm font-bold disabled:opacity-50">{step.type === 'speaking' ? (vi ? 'Đã luyện nói' : 'Practised speaking') : step.type === 'writing' ? (vi ? 'Nộp bài viết' : 'Submit writing') : (vi ? 'Kiểm tra' : 'Check')}</button>
          </>}
        </div>}
        <div className="mt-8 flex flex-wrap items-center justify-between gap-2 border-t border-stone-200 pt-4"><button type="button" onClick={() => void saveAndExit()} className="rounded-lg border-notebook px-4 py-2 text-xs">{vi ? 'Lưu và thoát' : 'Save and exit'}</button><div className="flex gap-2"><button type="button" disabled={stepIndex === 0} onClick={() => selectStep(journey.steps[stepIndex - 1])} className="rounded-lg border-notebook px-4 py-2 text-xs disabled:opacity-50"><ArrowLeftOutlined /> {vi ? 'Trước' : 'Previous'}</button><button type="button" disabled={stepIndex >= journey.steps.length - 1 || journey.steps[stepIndex + 1].status === 'Locked'} onClick={() => selectStep(journey.steps[stepIndex + 1])} className="rounded-lg border-notebook px-4 py-2 text-xs disabled:opacity-50">{vi ? 'Tiếp' : 'Next'} <ArrowRightOutlined /></button></div></div>
      </main>
    </div>
  </div>;
};
