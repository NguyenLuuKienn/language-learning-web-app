import React, { useEffect, useState } from 'react';
import { App } from 'antd';
import { ArrowLeftOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import { getErrorMessage, vocabularySessionsApi, type VocabularyLessonChoice, type VocabularySession } from '../../services/apiClient';
import { speechService } from '../../services/speechService';
import type { StudyTarget } from '../../types';
import { MatchingBoard } from './MatchingBoard';

interface Props {
  target: StudyTarget;
  onBack: () => void;
  lessonId?: string;
  onCompleted?: () => void;
}

export const VocabularyLearningView: React.FC<Props> = ({ target, onBack, lessonId, onCompleted }) => {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const { message } = App.useApp();
  const [choices, setChoices] = useState<VocabularyLessonChoice[]>([]);
  const [session, setSession] = useState<VocabularySession | null>(null);
  const [selectedLessonId, setSelectedLessonId] = useState('');
  const [answer, setAnswer] = useState('');
  const [matches, setMatches] = useState<Record<string, string>>({});
  const [selectedWord, setSelectedWord] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ correct: boolean; expected: string; next: VocabularySession } | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;
    vocabularySessionsApi.lessons()
      .then((result) => { if (active) setChoices(result); })
      .catch((error) => { if (active) message.error(getErrorMessage(error, vi ? 'Không tải được bài học.' : 'Could not load lessons.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [message, vi]);

  const step = session?.step;
  const play = (word: string, audioUrl?: string) => {
    if (audioUrl) {
      void new Audio(audioUrl).play().catch(() => speechService.speak(word, target.languageCode || 'en'));
    } else {
      speechService.speak(word, target.languageCode || 'en');
    }
  };
  const playFeedback = (correct: boolean) => {
    try {
      const context = new AudioContext();
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      oscillator.type = 'sine';
      oscillator.frequency.setValueAtTime(correct ? 660 : 240, context.currentTime);
      if (correct) oscillator.frequency.setValueAtTime(880, context.currentTime + 0.1);
      gain.gain.setValueAtTime(0.07, context.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, context.currentTime + 0.22);
      oscillator.connect(gain).connect(context.destination);
      oscillator.start(); oscillator.stop(context.currentTime + 0.23);
      oscillator.onended = () => { void context.close(); };
    } catch { /* Audio may be unavailable. */ }
  };

  useEffect(() => {
    if (!step || feedback || !['introduction', 'listenMeaning', 'dictation'].includes(step.kind)) return;
    const timer = window.setTimeout(() => play(step.word, step.audioUrl), 180);
    return () => window.clearTimeout(timer);
    // The step ID is the playback boundary; answer and matching changes must not replay audio.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step?.id, feedback]);

  const start = async (lessonId: string) => {
    setLoading(true);
    try {
      const result = await vocabularySessionsApi.start(lessonId);
      setSelectedLessonId(lessonId);
      setSession(result);
      setAnswer('');
      setMatches({});
      setSelectedWord(null);
      setFeedback(null);
    } catch (error) {
      message.error(getErrorMessage(error, vi ? 'Không thể bắt đầu phiên học.' : 'Could not start learning session.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (lessonId) void start(lessonId);
    // A course lesson ID is the session boundary.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lessonId]);

  const submit = async (value = answer) => {
    if (!session?.step || submitting) return;
    setSubmitting(true);
    try {
      const result = await vocabularySessionsApi.answer(session.id, session.step.id, value, matches);
      if (session.step.kind === 'introduction') {
        setSession(result.session);
        setAnswer('');
        setMatches({});
      } else { playFeedback(result.correct); setFeedback({ correct: result.correct, expected: result.expected, next: result.session }); }
    } catch (error) {
      message.error(getErrorMessage(error, vi ? 'Không lưu được câu trả lời.' : 'Could not save answer.'));
    } finally {
      setSubmitting(false);
    }
  };

  const next = () => {
    if (!feedback) return;
    const completed = feedback.next.completed;
    setSession(feedback.next);
    setAnswer('');
    setMatches({});
    setSelectedWord(null);
    setFeedback(null);
    if (completed) {
      void vocabularySessionsApi.lessons().then(setChoices).catch(() => undefined);
      onCompleted?.();
    }
  };

  const completePerfectRound = async () => {
    if (!session?.canCompleteAfterRoundOne || submitting) return;
    setSubmitting(true);
    try {
      const completed = await vocabularySessionsApi.completePerfectRound(session.id);
      setSession(completed);
      setFeedback(null);
      void vocabularySessionsApi.lessons().then(setChoices).catch(() => undefined);
      onCompleted?.();
    } catch (error) {
      message.error(getErrorMessage(error, vi ? 'Không thể hoàn thành vòng luyện.' : 'Could not complete the practice round.'));
    } finally {
      setSubmitting(false);
    }
  };

  const kindLabel = step?.kind === 'introduction' ? (vi ? 'Từ mới' : 'New word')
    : step?.kind === 'listenMeaning' ? (vi ? 'Nghe và chọn nghĩa' : 'Listen and choose the meaning')
    : step?.kind === 'cloze' ? (vi ? 'Điền vào chỗ trống' : 'Fill in the blank')
    : step?.kind === 'matching' ? (vi ? 'Ghép cặp từ và nghĩa' : 'Match words and meanings')
    : (vi ? 'Nghe và gõ lại từ' : 'Listen and type the word');

  return <div className="mx-auto max-w-5xl space-y-5">
    <button onClick={onBack} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold"><ArrowLeftOutlined /> {lessonId ? (vi ? 'Từ vựng' : 'Vocabulary') : (vi ? 'Ôn tập' : 'Review')}</button>
    <div className="rounded-2xl border-notebook-2 bg-emerald-900 p-5 text-white">
      <h1 className="text-lg font-bold">{choices.find((item) => item.lessonId === selectedLessonId)?.lessonTitle || (vi ? 'Học từ vựng theo nhịp của bạn' : 'Learn vocabulary at your pace')}</h1>
      <p className="mt-1 text-xs text-emerald-100">{choices.find((item) => item.lessonId === selectedLessonId)?.unitTitle || (vi ? 'Nghe, hiểu nghĩa và luyện tập lặp lại để ghi nhớ lâu.' : 'Listen, understand and practise in a loop for lasting recall.')}</p>
      {session && !session.completed && <div className="mt-4 flex flex-wrap gap-2 text-xs">
        <span className="rounded-lg bg-white/15 px-3 py-1.5">{vi ? 'Một phiên cho cả bài' : 'One session for this lesson'}</span>
        {!!session.words?.length && <span className="rounded-lg bg-white/15 px-3 py-1.5">{session.words.length} {vi ? 'từ' : 'words'}</span>}
        <span className="rounded-lg bg-white/15 px-3 py-1.5">{vi ? 'Đã làm' : 'Completed'} {session.position}/{session.totalSteps}</span>
        <span className="rounded-lg bg-white/15 px-3 py-1.5">{vi ? 'Trả lời đúng' : 'Correct answers'} {session.correctCount}</span>
      </div>}
    </div>
    {!session && <section className="rounded-2xl border-notebook-2 bg-white p-5">
      <h1 className="text-xl font-bold">{vi ? 'Học từ mới' : 'Learn new words'}</h1>
      <p className="mt-1 text-sm text-stone-600">{vi ? 'Mỗi bài là một phiên học hai vòng. Từ mới được giới thiệu theo cụm tối đa 3 từ; đúng hết vòng 1 có thể hoàn thành ngay.' : 'Each lesson is one two-round session. New words appear in batches of up to three; a perfect first round can finish immediately.'}</p>
      {loading && <p className="mt-4 text-sm">{vi ? 'Đang tải...' : 'Loading...'}</p>}
      {!loading && choices.filter((item) => item.completedGroups < item.availableGroups).length === 0 && <p className="mt-4 rounded-lg bg-[#FAF6EE] p-4 text-sm">{vi ? 'Hiện chưa có bài từ vựng mới phù hợp. Bạn có thể ôn các từ đã học hoặc chọn nội dung học khác.' : 'There are no new vocabulary lessons available. You can review learned words or choose another activity.'}</p>}
      <div className="mt-4 space-y-2">
        {choices.filter((item) => item.completedGroups < item.availableGroups).map((item) => <button key={item.lessonId} onClick={() => void start(item.lessonId)} className="w-full rounded-xl border-notebook bg-[#FAF6EE] p-3 text-left hover:bg-amber-50">
          <strong className="block text-sm">{item.lessonTitle}</strong>
          <span className="text-xs text-stone-600">{item.courseTitle} · {item.unitTitle} · {item.completedGroups}/{item.availableGroups} {vi ? 'bài từ vựng' : 'vocabulary lessons'}</span>
          {!!item.nextGroupWords?.length && <span className="mt-1 block text-xs text-stone-600">{vi ? 'Từ tiếp theo' : 'Next words'}: {item.nextGroupWords.join(' · ')}</span>}
        </button>)}
      </div>
    </section>}
    {session?.completed && <section className="rounded-2xl border-notebook-2 bg-white p-6 text-center">
      <h2 className="text-xl font-bold">{vi ? 'Hoàn thành phiên học từ vựng' : 'Vocabulary session complete'}</h2>
      <p className="mt-2 text-sm text-stone-600">{vi ? 'Các câu trả lời và tiến độ đã được lưu.' : 'Your answers and progress have been saved.'}</p>
      {!!session.words?.length && <p className="mt-2 text-sm font-semibold text-emerald-800">{session.words.join(' · ')}</p>}
      <button onClick={onBack} className="mt-4 rounded-lg bg-amber-300 px-4 py-2 text-sm font-bold">{lessonId ? (vi ? 'Về kho từ vựng' : 'Back to vocabulary') : (vi ? 'Về ôn tập' : 'Back to review')}</button>
    </section>}
    {session && !session.completed && step && <section className="mx-auto max-w-3xl rounded-2xl border-notebook-2 bg-white p-5 sm:p-7">
      <div className="flex items-center justify-between gap-2 text-xs text-stone-600">
        <strong>{kindLabel}</strong><span>{session.position + 1}/{session.totalSteps} · {step.round > 0 ? (vi ? 'Vòng ' : 'Round ') + step.round : ''}</span>
      </div>
      <div className="mt-3 h-2 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-amber-400" style={{ width: ((session.position / session.totalSteps) * 100) + '%' }} /></div>
      {session.canCompleteAfterRoundOne && <div className="mt-5 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">
        <p>{vi ? 'Bạn đã làm đúng toàn bộ vòng 1. Có thể hoàn thành bài ngay và lưu tiến độ cùng lịch ôn SRS.' : 'You got every round-one answer right. You can finish the lesson now and save SRS progress.'}</p>
        <button type="button" disabled={submitting} onClick={() => void completePerfectRound()} className="mt-3 rounded-lg bg-emerald-700 px-4 py-2 font-bold text-white disabled:opacity-50">{vi ? 'Bỏ qua vòng 2 và hoàn thành bài' : 'Skip round two and finish lesson'}</button>
      </div>}
      {step.kind === 'introduction' && <div className="mt-7 space-y-4 rounded-2xl border-2 border-amber-400 bg-[#FFFCF4] p-6 text-center sm:p-8">
        <h2 className="text-4xl font-black">{step.word}</h2>
        <p className="text-sm text-stone-500">{step.phonetic} {step.partOfSpeech && `· ${step.partOfSpeech}`}</p>
        <button type="button" onClick={() => play(step.word, step.audioUrl)} className="rounded-full border-notebook bg-white px-4 py-2 text-sm font-bold"><SoundOutlined /> {vi ? 'Nghe' : 'Listen'}</button>
        <p className="text-lg font-bold text-emerald-800">{step.meanings.length ? step.meanings.join('; ') : step.meaning}</p>
        {step.example && <div className="rounded-xl bg-[#FAF6EE] p-4 text-sm"><p>{step.example}</p><p className="mt-1 text-stone-600">{step.exampleTranslation}</p>
          <button type="button" onClick={() => play(step.example)} className="mt-2 rounded-lg border-notebook bg-white px-2 py-1 text-xs"><SoundOutlined /> {vi ? 'Nghe ví dụ' : 'Listen to example'}</button></div>}
      </div>}
      {(step.kind === 'listenMeaning' || step.kind === 'dictation') && <div className="mt-6 text-center">
        <button type="button" onClick={() => play(step.word, step.audioUrl)} className="rounded-lg border-notebook bg-amber-100 px-4 py-3 text-sm font-bold"><SoundOutlined /> {vi ? 'Nghe lại' : 'Replay'}</button>
      </div>}
      {step.kind === 'listenMeaning' && <div className="mt-5 grid gap-2">
        {step.options.map((option) => <button key={option} disabled={!!feedback || submitting} onClick={() => void submit(option)} className="rounded-xl border-notebook bg-[#FAF6EE] p-3 text-left text-sm hover:bg-amber-100 disabled:opacity-60">{option}</button>)}
      </div>}
      {step.kind === 'cloze' && <div className="mt-6 rounded-xl bg-[#FAF6EE] p-4">
        <p className="text-xs font-bold uppercase tracking-wide text-stone-500">{vi ? 'Hoàn thành câu theo nghĩa gợi ý' : 'Complete the sentence using the meaning below'}</p>
        <p className="mt-2 text-lg">{step.prompt}</p>
        {(step.meaning || step.partOfSpeech) && <p className="mt-3 text-sm font-semibold text-emerald-800">{vi ? 'Gợi ý' : 'Hint'}: {step.meaning}{step.partOfSpeech ? ` · ${step.partOfSpeech}` : ''}</p>}
        {step.exampleTranslation && <p className="mt-1 text-sm text-stone-600">{step.exampleTranslation}</p>}
      </div>}
      {(step.kind === 'cloze' || step.kind === 'dictation') && <input value={answer} disabled={!!feedback} onChange={(event) => setAnswer(event.target.value)} onKeyDown={(event) => { if (event.key === 'Enter' && answer.trim()) void submit(); }} placeholder={vi ? 'Nhập câu trả lời...' : 'Type your answer...'} className="mt-4 w-full rounded-lg border-notebook px-3 py-3 text-sm" />}
      {step.kind === 'matching' && <MatchingBoard words={step.matchWords} meanings={step.matchMeanings} matches={matches}
        selectedWord={selectedWord} expected={feedback?.expected} disabled={!!feedback} language={vi ? 'vi' : 'en'}
        onSelectWord={setSelectedWord} onMatch={(word, meaning) => {
          setMatches((current) => ({ ...Object.fromEntries(Object.entries(current).filter(([candidate, value]) => candidate === word || value !== meaning)), [word]: meaning }));
          setSelectedWord(null);
        }} />}
      {feedback && <div role="status" className={'mt-5 rounded-lg p-3 text-sm ' + (feedback.correct ? 'bg-emerald-50 text-emerald-800' : 'bg-rose-50 text-rose-800')}>
        <strong>{feedback.correct ? (vi ? 'Đúng!' : 'Correct!') : (vi ? 'Chưa đúng.' : 'Not quite.')}</strong>
        {!feedback.correct && feedback.expected && <p>{vi ? 'Đáp án: ' : 'Answer: '}{feedback.expected}</p>}
      </div>}
      <div className="mt-6 flex justify-end">
        {feedback
          ? <button onClick={next} className="rounded-lg bg-stone-900 px-4 py-2 text-sm font-bold text-white">{vi ? 'Tiếp tục' : 'Continue'}</button>
          : step.kind !== 'listenMeaning' && <button disabled={submitting || (step.kind === 'matching' ? step.matchWords.some((word) => !matches[word]) : step.kind !== 'introduction' && !answer.trim())} onClick={() => void submit()} className="w-full rounded-lg bg-emerald-700 px-4 py-3 text-sm font-bold text-white disabled:opacity-50">{step.kind === 'introduction' ? (vi ? 'Tiếp tục' : 'Continue') : (vi ? 'Kiểm tra' : 'Check')}</button>}
      </div>
    </section>}
  </div>;
};
