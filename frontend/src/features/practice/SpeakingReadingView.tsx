import { useEffect, useMemo, useRef, useState } from 'react';
import { ArrowLeftOutlined, AudioOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { curriculumStore } from '../../services/curriculumStore';
import { speechService } from '../../services/speechService';
import { SearchableSelect } from '../../components/ui/SearchableSelect';
import { SkillProgressBar } from './SkillProgressBar';
import { useSkillProgress } from './useSkillProgress';
import { playPracticeConfirmation } from '../../services/practiceFeedback';

interface SpeakingReadingViewProps { target: StudyTarget | null; onBack: () => void }

interface SpeechResult { results: ArrayLike<ArrayLike<{ transcript: string }>> }
interface BrowserRecognition {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  onresult: ((event: SpeechResult) => void) | null;
  onerror: (() => void) | null;
  onend: (() => void) | null;
  start: () => void;
  stop: () => void;
}

const speechLocale = (code: string) => ({ ja: 'ja-JP', zh: 'zh-CN', cn: 'zh-CN', ko: 'ko-KR', fr: 'fr-FR', de: 'de-DE', en: 'en-US' })[code.toLowerCase()] ?? 'en-US';

function dialogueTurns(phrase: string): Array<{ speaker: string; text: string }> {
  const markers = [...phrase.matchAll(/(?<!\p{L})([\p{Lu}][\p{L}'-]{0,20}(?:\s+[\p{Lu}][\p{L}'-]{0,20}){0,2}(?:\s*\([^)]{1,24}\))?):\s*/gu)];
  if (!markers.length) return [{ speaker: '', text: phrase }];
  return markers.map((marker, index) => ({
    speaker: marker[1].trim(),
    text: phrase.slice((marker.index ?? 0) + marker[0].length, markers[index + 1]?.index ?? phrase.length).trim()
  })).filter((turn) => turn.text);
}

export function SpeakingReadingView({ target, onBack }: SpeakingReadingViewProps) {
  const { t, language } = useTranslation();
  const [selectedId, setSelectedId] = useState('');
  const [transcript, setTranscript] = useState('');
  const [recording, setRecording] = useState(false);
  const recognitionRef = useRef<BrowserRecognition | null>(null);
  const phrases = useMemo(() => curriculumStore.getSpeakingForTarget(target).filter((item) => item.phrase?.trim()), [target]);
  const progress = useSkillProgress('speaking', phrases);
  const current = phrases.find((item) => item.id === selectedId) ?? phrases[0];
  useEffect(() => { if (progress.loaded && !selectedId && phrases.length) setSelectedId(progress.resumeId || phrases[0].id); }, [progress.loaded, progress.resumeId, selectedId, phrases]);
  useEffect(() => { if (progress.loaded && selectedId && current) void progress.record(current).catch(() => undefined); }, [progress.loaded, selectedId, current?.id]);

  useEffect(() => () => { recognitionRef.current?.stop(); speechService.stop(); }, []);

  const toggleRecording = () => {
    if (recording) { recognitionRef.current?.stop(); return; }
    const browser = window as typeof window & { SpeechRecognition?: new () => BrowserRecognition; webkitSpeechRecognition?: new () => BrowserRecognition };
    const Recognition = browser.SpeechRecognition ?? browser.webkitSpeechRecognition;
    if (!Recognition || !current) { setTranscript(t('speaking.recognitionUnavailable')); return; }
    const recognition = new Recognition();
    recognition.lang = speechLocale(current.languageCode);
    recognition.continuous = false;
    recognition.interimResults = false;
    recognition.onresult = (event) => {
      const heard = event.results[0][0].transcript;
      setTranscript(heard);
      if (heard.trim()) { playPracticeConfirmation(); void progress.record(current, undefined, true).catch(() => undefined); }
    };
    recognition.onerror = () => { setRecording(false); setTranscript(t('speaking.recognitionFailed')); };
    recognition.onend = () => setRecording(false);
    recognitionRef.current = recognition;
    setTranscript('');
    recognition.start();
    setRecording(true);
  };

  return <div className="mx-auto max-w-4xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      <button type="button" onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {t('common.back')}</button>
      <div><h1 className="text-xl font-bold">{t('speaking.headerTitle')}</h1><p className="text-xs text-stone-600">{target?.languageName ?? ''} · {target?.level ?? ''}</p></div>
    </header>
    {!target && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</p>}
    {target && phrases.length === 0 && <p className="rounded-xl border-notebook bg-white p-6 text-sm">{t('speaking.noPhrases')}</p>}
    {target && <SkillProgressBar completed={progress.completed} practiced={progress.practiced} pendingAssessment total={phrases.length} language={language} error={progress.error} hasResume={!!progress.resumeId} onResume={() => setSelectedId(progress.resumeId)} />}
    {current && <section className="space-y-5 rounded-xl border-notebook bg-white p-5">
      <SearchableSelect value={current.id} onChange={(id) => { recognitionRef.current?.stop(); setSelectedId(id); setTranscript(''); }} options={phrases.map((item) => ({ value: item.id, label: item.title }))} label={t('speaking.headerTitle')} />
      <p className="text-xs text-stone-600">{current.courseTitle} · {current.level}</p>
      <div className="rounded-lg bg-amber-50 p-5">
        {current.context && <p className="mb-2 text-sm text-stone-600">{current.context}</p>}
        <div className="space-y-3">{dialogueTurns(current.phrase).map((turn, index) => <div key={`${current.id}-${index}`} className="rounded-lg border border-amber-200 bg-white/70 p-3">
          {turn.speaker && <span className="mb-1 block text-xs font-bold text-amber-900">{turn.speaker}</span>}
          <p className="text-lg font-bold">{turn.text}</p>
        </div>)}</div>
        {current.phonetic && <p className="mt-2 text-sm text-stone-500">{current.phonetic}</p>}
        <p className="mt-2 text-sm text-stone-600">{t('speaking.translationLabel')}: {current.translation}</p>
      </div>
      <div className="flex flex-wrap gap-2">
        <button type="button" onClick={() => speechService.speak(current.phrase, current.languageCode)} className="rounded-lg bg-stone-900 px-4 py-2 text-sm font-bold text-white"><SoundOutlined /> {t('speaking.listenSample')}</button>
        <button type="button" onClick={toggleRecording} className="rounded-lg border border-stone-300 px-4 py-2 text-sm font-bold"><AudioOutlined /> {t(recording ? 'speaking.stopRecording' : 'speaking.startRecording')}</button>
      </div>
      <p className="text-xs text-stone-600">{t('speaking.transcriptHint')}</p>
      {transcript && <div role="status" className="rounded-lg bg-emerald-50 p-4 text-sm motion-safe:animate-pulse"><strong>{t('speaking.recognitionHeader')}</strong><p className="mt-2">{transcript}</p></div>}
    </section>}
  </div>;
}
