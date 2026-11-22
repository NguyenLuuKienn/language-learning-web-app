import { useEffect, useMemo, useRef, useState } from 'react';
import { ArrowLeftOutlined, AudioOutlined, SoundOutlined } from '@ant-design/icons';
import { getErrorMessage, specialPracticeApi, type SpecialPracticeAttempt, type SpecialPracticeCatalogItem } from '../../services/apiClient';
import { useTranslation } from '../../i18n';
import { speechService } from '../../services/speechService';
import type { StudyTarget } from '../../types';

type PracticeKind = 'certificate' | 'interview';
interface Props { kind: PracticeKind; target: StudyTarget | null; onBack: () => void }

interface BrowserRecognition {
  lang: string;
  onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
  onerror: (() => void) | null;
  start(): void;
}

export function SpecialPracticeView({ kind, target, onBack }: Props) {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const [catalog, setCatalog] = useState<SpecialPracticeCatalogItem[]>([]);
  const [attempt, setAttempt] = useState<SpecialPracticeAttempt | null>(null);
  const [certificate, setCertificate] = useState<'IELTS' | 'TOEIC'>('IELTS');
  const [index, setIndex] = useState(0);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [response, setResponse] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [reloadCatalog, setReloadCatalog] = useState(0);
  const [now, setNow] = useState(0);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const [recording, setRecording] = useState(false);

  useEffect(() => () => {
    const recorder = recorderRef.current;
    if (recorder) {
      recorder.onstop = null;
      if (recorder.state === 'recording') recorder.stop();
      recorder.stream.getTracks().forEach(track => track.stop());
    }
  }, []);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    specialPracticeApi.catalog().then((items) => { if (active) setCatalog(items); })
      .catch(() => { if (active) { setCatalog([]); setError(vi ? 'Chưa tải được danh sách bài luyện. Vui lòng thử lại.' : 'Could not load practice lessons. Please try again.'); } })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [kind, vi, reloadCatalog]);

  const available = useMemo(() => catalog.filter((item) => item.kind === kind &&
    (kind !== 'certificate' || item.certificate === certificate)), [catalog, kind, certificate]);
  const question = attempt?.questions[index];
  const questionId = question?.id;
  const savedAnswer = question ? attempt?.answers[question.id] : undefined;
  const attemptId = attempt?.id;
  const attemptStatus = attempt?.status;
  const answeredCount = attempt ? Object.keys(attempt.answers).length : 0;
  const allAnswered = !!attempt && answeredCount === attempt.questions.length;
  const remainingSeconds = attempt?.deadlineAt ? Math.max(0, Math.ceil((Date.parse(attempt.deadlineAt) - now) / 1000)) : null;

  useEffect(() => {
    if (!attempt?.deadlineAt || attempt.status !== 'InProgress') return;
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [attempt?.id, attempt?.deadlineAt, attempt?.status]);

  useEffect(() => {
    if (!attemptId || attemptStatus !== 'InProgress' || remainingSeconds !== 0) return;
    if (recording) { recorderRef.current?.stop(); setRecording(false); setSaving(true); return; }
    if (saving) return;
    let active = true;
    specialPracticeApi.submit(attemptId).then(result => { if (active) setAttempt(result); })
      .catch(cause => { if (active) setError(getErrorMessage(cause, vi ? 'Không nộp được bài khi hết giờ.' : 'Could not submit when time expired.')); });
    return () => { active = false; };
  }, [attemptId, attemptStatus, remainingSeconds, recording, saving, vi]);

  useEffect(() => {
    if (!questionId) return;
    setSelectedIndex(savedAnswer?.selectedIndex ?? null);
    setResponse(savedAnswer?.text ?? '');
  }, [attemptId, index, questionId, savedAnswer?.selectedIndex, savedAnswer?.text]);

  const open = async (item: SpecialPracticeCatalogItem) => {
    setSaving(true); setError('');
    try {
      const next = item.activeAttemptId
        ? await specialPracticeApi.get(item.activeAttemptId)
        : await specialPracticeApi.start(item.courseId, item.contentId, item.kind);
      setAttempt(next);
      const firstUnanswered = next.questions.findIndex((entry) => !next.answers[entry.id]);
      setIndex(firstUnanswered < 0 ? 0 : firstUnanswered);
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không thể mở bài luyện.' : 'Could not open practice.')); }
    finally { setSaving(false); }
  };

  const save = async (moveNext = false) => {
    if (!attempt || !question || saving || attempt.status !== 'InProgress') return;
    if (question.options.length ? selectedIndex === null : !response.trim() && !attempt.answers[question.id]?.audioUrl) return;
    setSaving(true); setError('');
    try {
      const updated = await specialPracticeApi.answer(attempt.id, question.id,
        question.options.length ? selectedIndex : null, question.options.length ? null : response.trim());
      setAttempt(updated);
      if (moveNext) setIndex((current) => Math.min(current + 1, updated.questions.length - 1));
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không lưu được câu trả lời.' : 'Could not save the answer.')); }
    finally { setSaving(false); }
  };

  const submit = async () => {
    if (!attempt || saving || !allAnswered) return;
    setSaving(true); setError('');
    try { setAttempt(await specialPracticeApi.submit(attempt.id)); }
    catch (cause) { setError(getErrorMessage(cause, vi ? 'Không nộp được bài.' : 'Could not submit.')); }
    finally { setSaving(false); }
  };

  const listen = (script: string, url: string) => {
    if (url) void new Audio(url).play().catch(() => speechService.speak(script, target?.languageCode || 'en'));
    else speechService.speak(script, target?.languageCode || 'en');
  };

  const dictate = () => {
    const browser = window as typeof window & {
      SpeechRecognition?: new () => BrowserRecognition; webkitSpeechRecognition?: new () => BrowserRecognition
    };
    const Recognition = browser.SpeechRecognition ?? browser.webkitSpeechRecognition;
    if (!Recognition) { setError(vi ? 'Trình duyệt không hỗ trợ chuyển giọng nói thành văn bản.' : 'Speech transcription is unavailable in this browser.'); return; }
    const recorder = new Recognition();
    recorder.lang = target?.languageCode === 'en' ? 'en-US' : target?.languageCode || 'en-US';
    recorder.onresult = (event) => setResponse(event.results[0]?.[0]?.transcript ?? '');
    recorder.onerror = () => setError(vi ? 'Không nhận được giọng nói. Bạn có thể nhập câu trả lời.' : 'Speech was not recognized. You can type your response.');
    recorder.start();
  };

  const toggleRecording = async () => {
    if (recorderRef.current?.state === 'recording') { setSaving(true); recorderRef.current.stop(); setRecording(false); return; }
    if (!attempt || !question || !navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === 'undefined') {
      setError(vi ? 'Trình duyệt không hỗ trợ ghi âm.' : 'Audio recording is unavailable.'); return;
    }
    try {
      const mimeType = ['audio/webm', 'audio/ogg'].find(type => MediaRecorder.isTypeSupported(type));
      if (!mimeType) throw new Error(vi ? 'Định dạng ghi âm chưa được hỗ trợ.' : 'Recording format is unsupported.');
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream, { mimeType, audioBitsPerSecond: 64000 });
      const chunks: BlobPart[] = [];
      recorder.ondataavailable = event => { if (event.data.size) chunks.push(event.data); };
      recorder.onstop = () => {
        stream.getTracks().forEach(track => track.stop()); recorderRef.current = null;
        const blob = new Blob(chunks, { type: mimeType });
        if (!blob.size) return;
        setSaving(true);
        void specialPracticeApi.uploadAudio(attempt.id, question.id, blob)
          .then(updated => setAttempt(updated))
          .catch(cause => setError(getErrorMessage(cause, vi ? 'Không lưu được bản ghi âm.' : 'Could not save recording.')))
          .finally(() => setSaving(false));
      };
      recorderRef.current = recorder; recorder.start(); setRecording(true); setError('');
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không mở được micro.' : 'Could not access microphone.')); }
  };

  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="flex flex-wrap items-center gap-3 border-b border-stone-300 pb-3">
      <button type="button" disabled={recording || saving} onClick={attempt ? () => setAttempt(null) : onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold disabled:opacity-50"><ArrowLeftOutlined /> {vi ? 'Quay lại' : 'Back'}</button>
      <div><h1 className="text-xl font-bold">{kind === 'certificate' ? (vi ? 'Luyện thi chứng chỉ' : 'Certificate practice') : (vi ? 'Luyện phỏng vấn' : 'Interview practice')}</h1>
        <p className="text-xs text-stone-600">{target?.languageName} · {target?.level}</p></div>
    </header>
    {error && <div role="alert" className="rounded-lg border border-rose-300 bg-rose-50 p-3 text-sm text-rose-800">{error}{!attempt && !loading && <button type="button" onClick={() => setReloadCatalog(value => value + 1)} className="ml-3 font-bold underline">{vi ? 'Thử lại' : 'Retry'}</button>}</div>}
    {!attempt && <>
      {kind === 'certificate' && <div className="flex gap-2" role="tablist" aria-label={vi ? 'Chứng chỉ' : 'Certificate'}>
        {(['IELTS', 'TOEIC'] as const).map((value) => <button type="button" role="tab" aria-selected={certificate === value} key={value} onClick={() => setCertificate(value)} className={`rounded-lg border-notebook px-4 py-2 text-sm font-bold ${certificate === value ? 'bg-amber-300' : 'bg-white'}`}>{value}</button>)}
      </div>}
      {loading && <p className="text-sm">{vi ? 'Đang tải...' : 'Loading...'}</p>}
      {!loading && !error && available.length === 0 && <section className="rounded-xl border-notebook bg-white p-6 text-sm">
        <h2 className="font-bold">{vi ? 'Chưa có bài luyện phù hợp' : 'No matching practice lessons yet'}</h2>
        <p className="mt-2 text-stone-600">{kind === 'certificate'
          ? (vi ? `Hiện chưa có đề ${certificate} cho trình độ này. Bạn có thể quay lại sau hoặc học tiếp các kỹ năng.` : `There is no ${certificate} practice test for this level yet. You can return later or keep practising your skills.`)
          : (vi ? 'Hiện chưa có bài phỏng vấn phù hợp với mục tiêu của bạn. Bạn có thể quay lại sau hoặc học tiếp các kỹ năng.' : 'There is no interview practice for your goal yet. You can return later or keep practising your skills.')}</p>
      </section>}
      <div className="grid gap-3 sm:grid-cols-2">{available.map((item) => <article key={item.contentId} className="rounded-xl border-notebook bg-white p-5">
        <span className="text-xs font-bold text-amber-800">{item.certificate || item.profession} · {item.level}</span>
        <h2 className="mt-2 text-lg font-bold">{item.title}</h2>
        {item.archived && <p className="mt-1 text-xs text-stone-600">{vi ? 'Đề đã gỡ; bài làm cũ vẫn xem được.' : 'Content removed; your existing attempt remains available.'}</p>}
        <p className="mt-1 text-xs text-stone-600">{item.questionCount} {vi ? 'câu/lượt' : 'questions/turns'}</p>
        {item.lastStatus === 'PendingReview' && <p className="mt-2 text-xs text-amber-800">{vi ? 'Bài trước đang chờ đánh giá' : 'Previous attempt awaiting review'}</p>}
        {(!item.archived || item.activeAttemptId) && <button type="button" disabled={saving} onClick={() => void open(item)} className="mt-4 rounded-lg bg-amber-300 px-4 py-2 text-sm font-bold disabled:opacity-50">{item.activeAttemptId ? (vi ? 'Tiếp tục' : 'Continue') : (vi ? 'Bắt đầu' : 'Start')}</button>}
        {item.lastAttemptId && !item.activeAttemptId && <button type="button" disabled={saving} onClick={() => void specialPracticeApi.get(item.lastAttemptId!).then((previous) => { setAttempt(previous); setIndex(0); }).catch((cause) => setError(getErrorMessage(cause, vi ? 'Không mở được kết quả.' : 'Could not open the result.')))} className="ml-2 mt-4 rounded-lg border-notebook px-4 py-2 text-sm disabled:opacity-50">{vi ? 'Kết quả gần nhất' : 'Latest result'}</button>}
      </article>)}</div>
    </>}
    {attempt && attempt.status !== 'InProgress' && <section className="rounded-xl border-notebook-2 bg-white p-6">
      <h2 className="text-lg font-bold">{vi ? 'Đã nộp bài' : 'Submitted'}</h2>
      {attempt.objectiveMaxScore > 0 && <p className="mt-2 text-sm">{vi ? 'Điểm câu khách quan' : 'Objective question score'}: {attempt.objectiveScore}/{attempt.objectiveMaxScore}</p>}
      {attempt.status === 'PendingReview' && <p className="mt-2 text-sm text-amber-800">{vi ? 'Phần nói/viết hoặc phỏng vấn đang chờ đánh giá. Chưa có điểm chứng chỉ chính thức.' : 'Speaking, writing or interview responses await review. No official certificate score is available.'}</p>}
      {attempt.status === 'Reviewed' && <><p className="mt-2 text-sm">{vi ? 'Điểm câu tự luận' : 'Open-response score'}: {attempt.subjectiveScore}/{attempt.subjectiveMaxScore}</p><p className="mt-2 whitespace-pre-wrap rounded-lg bg-[#FAF6EE] p-3 text-sm">{attempt.feedback}</p></>}
      {(attempt.status === 'Scored' || attempt.status === 'Reviewed') && <p className="mt-2 text-xs text-stone-600">{vi ? 'Đây là điểm bài luyện theo số câu đã nhập, không phải band IELTS hay điểm TOEIC quy đổi.' : 'This is the raw practice score, not an IELTS band or a scaled TOEIC score.'}</p>}
      <button type="button" onClick={() => setAttempt(null)} className="mt-4 rounded-lg border-notebook px-4 py-2 text-sm">{vi ? 'Về danh sách' : 'Back to catalog'}</button>
    </section>}
    {attempt?.status === 'InProgress' && question && <section className="rounded-xl border-notebook-2 bg-white p-6">
      <div className="flex items-center justify-between gap-3 text-xs text-stone-600"><strong>{attempt.title} · {question.skill}</strong><span>{remainingSeconds !== null && <span role="timer" className="mr-3 font-bold text-rose-700">{Math.floor(remainingSeconds / 60)}:{String(remainingSeconds % 60).padStart(2, '0')}</span>}{index + 1}/{attempt.questions.length}</span></div>
      <div className="mt-3 h-2 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-amber-400" style={{ width: `${(index + 1) / attempt.questions.length * 100}%` }} /></div>
      {question.passage && <p className="mt-5 whitespace-pre-wrap rounded-lg bg-[#FAF6EE] p-4 text-sm">{question.passage}</p>}
      {(question.audioScript || question.audioUrl) && <button type="button" onClick={() => listen(question.audioScript, question.audioUrl)} className="mt-5 rounded-lg border-notebook bg-amber-100 px-4 py-2 text-sm font-bold"><SoundOutlined /> {vi ? 'Nghe' : 'Listen'}</button>}
      <h2 className="mt-5 text-lg font-bold">{question.prompt}</h2>
      {question.options.length > 0 ? <div className="mt-5 grid gap-2">{question.options.map((option, optionIndex) => <button type="button" key={optionIndex} aria-pressed={selectedIndex === optionIndex} onClick={() => setSelectedIndex(optionIndex)} className={`rounded-lg border-notebook p-3 text-left text-sm ${selectedIndex === optionIndex ? 'bg-amber-100 font-bold' : 'bg-white'}`}>{option}</button>)}</div>
        : <div className="mt-5 space-y-2">{(question.skill === 'speaking' || kind === 'interview') && <div className="flex flex-wrap gap-2"><button type="button" onClick={dictate} className="rounded-lg border-notebook px-3 py-2 text-sm"><AudioOutlined /> {vi ? 'Nói để nhập câu trả lời' : 'Speak to transcribe'}</button><button type="button" disabled={saving} onClick={() => void toggleRecording()} className="rounded-lg border-notebook bg-amber-100 px-3 py-2 text-sm">{recording ? (vi ? 'Dừng ghi âm' : 'Stop recording') : (vi ? 'Ghi âm bài nói' : 'Record speaking')}</button></div>}
          {attempt.answers[question.id]?.audioUrl && <audio controls src={attempt.answers[question.id].audioUrl!} className="w-full" aria-label={vi ? 'Bản ghi âm đã lưu' : 'Saved recording'} />}
          <textarea value={response} onChange={(event) => setResponse(event.target.value)} rows={6} maxLength={10000} aria-label={vi ? 'Câu trả lời' : 'Your answer'} className="w-full rounded-lg border-notebook p-3 text-sm" placeholder={vi ? 'Nhập hoặc đọc câu trả lời...' : 'Type or speak your answer...'} />
          <p className="text-xs text-stone-600">{vi ? 'Có thể nộp bản ghi âm hoặc văn bản. Bản chép lời không dùng để chấm phát âm.' : 'You can submit audio or text. Transcription does not score pronunciation.'}</p></div>}
      <div className="mt-5 flex flex-wrap items-center justify-between gap-2">
        <button type="button" disabled={index === 0 || saving || recording} onClick={() => setIndex(index - 1)} className="rounded-lg border-notebook px-4 py-2 text-sm disabled:opacity-50">{vi ? 'Câu trước' : 'Previous'}</button>
        <div className="flex gap-2"><button type="button" disabled={saving || recording || (question.options.length > 0 ? selectedIndex === null : !response.trim() && !attempt.answers[question.id]?.audioUrl)} onClick={() => void save(index < attempt.questions.length - 1)} className="rounded-lg bg-amber-300 px-4 py-2 text-sm font-bold disabled:opacity-50">{index < attempt.questions.length - 1 ? (vi ? 'Lưu và tiếp' : 'Save and next') : (vi ? 'Lưu câu trả lời' : 'Save answer')}</button>
          {allAnswered && <button type="button" disabled={saving} onClick={() => void submit()} className="rounded-lg bg-stone-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-50">{vi ? 'Nộp bài' : 'Submit'}</button>}</div>
      </div>
    </section>}
  </div>;
}
