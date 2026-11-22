import React, { useState, useEffect, useRef } from 'react';
import { message } from 'antd';
import { useTranslation } from '../../i18n';
import { quizApi } from '../../services/apiClient';
import type { QuizQuestion, QuizAnswerSubmission, QuizResult } from '../../types';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  ArrowLeftOutlined,
  ReloadOutlined,
  LoadingOutlined
} from '@ant-design/icons';

interface QuizPlayerViewProps {
  lessonId: string;
  lessonTitle: string;
  onBack: () => void;
  onResult?: () => void;
  initialDraft?: string;
  onDraft?: (draft: string, cursor: number) => void;
}

export const QuizPlayerView: React.FC<QuizPlayerViewProps> = ({
  lessonId,
  lessonTitle,
  onBack,
  onResult,
  initialDraft,
  onDraft
}) => {
  const { t } = useTranslation();
  const [questions, setQuestions] = useState<QuizQuestion[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, QuizAnswerSubmission>>({});
  const [currentTextAnswer, setCurrentTextAnswer] = useState('');
  const [matchingPairs, setMatchingPairs] = useState<Record<string, string>>({});
  const [currentSelectedOption, setCurrentSelectedOption] = useState<string | null>(null);
  const [showExplanation, setShowExplanation] = useState(false);
  const [quizResult, setQuizResult] = useState<QuizResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const clientAttemptId = useRef<string>(crypto.randomUUID());

  const loadQuiz = React.useCallback(async (fresh = false) => {
    setIsLoading(true);
    setQuizResult(null);
    setShowExplanation(false);
    let restored: { currentIndex?: number; answers?: Record<string, QuizAnswerSubmission>; clientAttemptId?: string } = {};
    try { restored = fresh ? {} : JSON.parse(initialDraft || '{}'); } catch { /* Discard an invalid checkpoint. */ }
    if (restored.clientAttemptId) clientAttemptId.current = restored.clientAttemptId;
    setCurrentIndex(Number.isInteger(restored.currentIndex) ? Math.max(0, restored.currentIndex!) : 0);
    setAnswers(restored.answers && typeof restored.answers === 'object' ? restored.answers : {});
    setMatchingPairs({});
    setError('');
    try {
      const data = await quizApi.getLessonQuiz(lessonId);
      if (data && data.length > 0) {
        setQuestions(data);
        setCurrentIndex(index => Math.min(index, data.length - 1));
      } else {
        throw new Error('No questions found');
      }
    } catch (cause) {
      setQuestions([]);
      setError(cause instanceof Error ? cause.message : t('common.error'));
    } finally {
      setIsLoading(false);
    }
  }, [lessonId, t]);

  useEffect(() => {
    void loadQuiz();
  }, [loadQuiz]);

  const handleSelectOption = (optionId: string) => {
    setCurrentSelectedOption(optionId);
  };

  const handleNextQuestion = () => {
    const currentQ = questions[currentIndex];
    const newSubmission: QuizAnswerSubmission = {
      questionId: currentQ.id,
      selectedOptionId: currentSelectedOption || undefined,
      textAnswer: currentTextAnswer.trim() || undefined,
      matchingJson: currentQ.matchingTerms?.length ? JSON.stringify({ pairs: matchingPairs }) : undefined
    };

    const updatedAnswers = { ...answers, [currentQ.id]: newSubmission };
    setAnswers(updatedAnswers);

    if (currentIndex + 1 < questions.length) {
      onDraft?.(JSON.stringify({ currentIndex: currentIndex + 1, answers: updatedAnswers, clientAttemptId: clientAttemptId.current }), currentIndex + 1);
      setCurrentIndex((prev) => prev + 1);
      setCurrentSelectedOption(null);
      setCurrentTextAnswer('');
      setMatchingPairs({});
      setShowExplanation(false);
    } else {
      onDraft?.(JSON.stringify({ currentIndex, answers: updatedAnswers, clientAttemptId: clientAttemptId.current }), currentIndex);
      handleSubmitAll(Object.values(updatedAnswers));
    }
  };

  const handleSubmitAll = async (submissionList: QuizAnswerSubmission[]) => {
    setIsSubmitting(true);
    try {
      const result = await quizApi.submitQuiz(lessonId, submissionList, clientAttemptId.current);
      setQuizResult(result);
      onDraft?.('{}', 0);
      onResult?.();
      if (result.passed) {
        message.success(t('quiz.passed'));
      } else {
        message.info(t('quiz.failed'));
      }
    } catch (cause) {
      const detail = cause instanceof Error ? cause.message : t('common.error');
      setError(detail);
      message.error(detail);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <LoadingOutlined className="text-3xl text-stone-500 animate-spin" />
      </div>
    );
  }

  if (questions.length === 0) {
    return <div className="max-w-lg mx-auto bg-white border-notebook-2 rounded-2xl p-6 text-center space-y-4">
      <p className="text-sm text-rose-700">{error || t('quiz.noQuestions')}</p>
      <button onClick={() => void loadQuiz()} className="px-4 py-2 border-notebook rounded-lg">{t('quiz.retryQuiz')}</button>
      <button onClick={onBack} className="px-4 py-2 border-notebook rounded-lg ml-2">{t('common.back')}</button>
    </div>;
  }

  if (quizResult) {
    return (
      <div className="max-w-lg mx-auto bg-white border-notebook-2 rounded-2xl p-8 shadow-lg text-center space-y-6">
        <div
          className={`w-20 h-20 rounded-full flex items-center justify-center mx-auto text-4xl border-2 ${
            quizResult.passed
              ? 'bg-emerald-50 text-emerald-600 border-emerald-400'
              : 'bg-rose-50 text-rose-600 border-rose-400'
          }`}
        >
          {quizResult.passed ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
        </div>

        <div>
          <span
            className={`text-xs font-mono font-bold px-3 py-1 rounded-full border ${
              quizResult.passed
                ? 'bg-emerald-100 text-emerald-800 border-emerald-300'
                : 'bg-rose-100 text-rose-800 border-rose-300'
            }`}
          >
            {quizResult.passed ? t('quiz.passed') : t('quiz.failed')}
          </span>
          <h2 className="text-2xl font-bold text-stone-900 mt-3">{t('quiz.quizResults')}</h2>
          <p className="text-xs text-stone-500 mt-1">{lessonTitle}</p>
        </div>

        <div className="grid grid-cols-2 gap-4 bg-stone-50 p-4 rounded-xl border border-stone-200">
          <div>
            <span className="text-[11px] font-bold uppercase tracking-wider text-stone-400 block">
              {t('quiz.score')}
            </span>
            <span className="text-2xl font-black text-stone-900">
              {quizResult.percentage}%
            </span>
          </div>
          <div>
            <span className="text-[11px] font-bold uppercase tracking-wider text-stone-400 block">
              {t('common.all')}
            </span>
            <span className="text-2xl font-black text-stone-900">
              {quizResult.score} / {quizResult.maxScore}
            </span>
          </div>
        </div>

        <div className="flex gap-3">
          <button
            onClick={() => { clientAttemptId.current = crypto.randomUUID(); onDraft?.('{}', 0); void loadQuiz(true); }}
            className="flex-1 py-2.5 rounded-lg border-notebook bg-white hover:bg-stone-100 text-stone-800 font-bold text-xs flex items-center justify-center gap-2 transition cursor-pointer notebook-btn"
          >
            <ReloadOutlined />
            <span>{t('quiz.retryQuiz')}</span>
          </button>
          <button
            onClick={onBack}
            className="flex-1 py-2.5 rounded-lg bg-stone-900 text-white font-bold text-xs notebook-btn hover:bg-stone-800 transition cursor-pointer"
          >
            {t('quiz.returnToLesson')}
          </button>
        </div>
      </div>
    );
  }

  const currentQ = questions[currentIndex];
  const progressPercent = Math.round(((currentIndex + 1) / questions.length) * 100);

  // Robust QuestionType detection (handles backend enum numbers 1, 2, 3 as well as strings)
  const isMatching =
    currentQ.type === 'Matching' ||
    String(currentQ.type) === '3' ||
    String(currentQ.type).toLowerCase() === 'matching';

  const isMultipleChoice = !isMatching && (
    currentQ.type === 'MultipleChoice' ||
    (currentQ.type as unknown) === 1 ||
    String(currentQ.type) === '1' ||
    String(currentQ.type).toLowerCase() === 'multiplechoice' ||
    (currentQ.options && currentQ.options.length > 0));

  const isFillInBlank =
    !isMultipleChoice &&
    (currentQ.type === 'FillInBlank' ||
      (currentQ.type as unknown) === 2 ||
      String(currentQ.type) === '2' ||
      String(currentQ.type).toLowerCase() === 'fillinblank' ||
      currentQ.prompt.includes('_____'));

  const questionBadgeText = isMultipleChoice
    ? t('quiz.multipleChoice')
    : isFillInBlank
    ? t('quiz.fillInBlank')
    : isMatching
    ? t('quiz.matching')
    : t('quiz.title');
  return (
    <div className="max-w-xl mx-auto space-y-6">
      {error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-xs text-rose-700">{error}</p>}
      <div className="flex items-center justify-between pb-3 border-b-2 border-stone-300">
        <button
          onClick={onBack}
          className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold text-stone-700 hover:bg-stone-200 transition notebook-btn bg-white"
        >
          <ArrowLeftOutlined />
          <span>{t('common.back')}</span>
        </button>
        <span className="text-xs font-bold text-stone-600">
          {t('quiz.questionOf', {
            current: currentIndex + 1,
            total: questions.length
          })}
        </span>
      </div>

      <div className="w-full bg-stone-200 h-2 rounded-full overflow-hidden border border-stone-300">
        <div
          className="bg-amber-400 h-full transition-all duration-300 rounded-full"
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      <div className="bg-white border-notebook-2 rounded-2xl p-6 shadow-md space-y-6">
        <div>
          <span className="text-[11px] font-mono font-bold uppercase tracking-wider text-amber-900 bg-amber-100 px-2.5 py-1 rounded-md border border-amber-300 inline-block">
            {questionBadgeText}
          </span>
          <h2 className="text-lg font-bold text-stone-900 mt-3 leading-snug">
            {currentQ.prompt}
          </h2>
        </div>

        {isMultipleChoice && (
          <div className="space-y-2.5">
            {currentQ.options.map((opt) => (
              <button
                key={opt.id}
                onClick={() => handleSelectOption(opt.id)}
                className={`w-full p-3.5 rounded-xl border-notebook text-left font-semibold text-xs transition flex items-center justify-between cursor-pointer ${
                  currentSelectedOption === opt.id
                    ? 'bg-amber-200/80 border-[#262626] text-stone-900 shadow-2xs font-bold'
                    : 'bg-white hover:bg-stone-50 text-stone-700'
                }`}
              >
                <span>{opt.content}</span>
                <span
                  className={`w-4 h-4 rounded-full border border-stone-400 flex items-center justify-center text-[10px] ${
                    currentSelectedOption === opt.id
                      ? 'bg-stone-900 border-stone-900 text-white'
                      : ''
                  }`}
                >
                  {currentSelectedOption === opt.id && '✓'}
                </span>
              </button>
            ))}
          </div>
        )}

        {isFillInBlank && (
          <div className="space-y-3">
            <label htmlFor="quiz-fill-answer" className="block text-xs font-bold text-stone-700">{t('quiz.fillInBlank')}</label>
            <input
              id="quiz-fill-answer"
              type="text"
              value={currentTextAnswer}
              onChange={(event) => setCurrentTextAnswer(event.target.value)}
              onKeyDown={(event) => { if (event.key === 'Enter' && currentTextAnswer.trim()) handleNextQuestion(); }}
              placeholder={t('quiz.fillAnswerPlaceholder')}
              className="w-full rounded-xl border-notebook bg-amber-50/30 px-4 py-3 text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-amber-400"
            />
            <p className="text-xs text-stone-500">{t('quiz.fillAnswerHint')}</p>
          </div>
        )}
        {isMatching && (
          <div className="space-y-3">
            {(currentQ.matchingTerms ?? []).map((term) => <label key={term} className="flex items-center gap-3 text-sm">
              <span className="min-w-0 flex-1 font-semibold">{term}</span>
              <select value={matchingPairs[term] ?? ''} onChange={(event) => setMatchingPairs((pairs) => ({ ...pairs, [term]: event.target.value }))} className="min-w-0 flex-1 rounded-lg border-notebook bg-white p-2" aria-label={`${t('quiz.matchTerm')} ${term}`}>
                <option value="">{t('quiz.chooseMatch')}</option>
                {(currentQ.matchingChoices ?? []).map((choice, index) => <option key={`${choice}-${index}`} value={choice}>{choice}</option>)}
              </select>
            </label>)}
          </div>
        )}
        {showExplanation && (
          <div className="p-3.5 bg-amber-50 border border-amber-300 rounded-xl text-xs space-y-1">
            <span className="font-bold text-amber-900 block">{t('quiz.explanation')}</span>
            <p className="text-amber-800 leading-relaxed">{currentQ.explanation}</p>
          </div>
        )}

        <div className="pt-2 flex gap-3">
          {!showExplanation && currentQ.explanation && (
            <button
              onClick={() => setShowExplanation(true)}
              className="px-4 py-2.5 border-notebook rounded-xl bg-white hover:bg-stone-50 text-xs font-bold text-stone-700 transition"
            >
              {t('quiz.explanation')}
            </button>
          )}

          <button
            onClick={handleNextQuestion}
            disabled={
              isSubmitting ||
              (isMultipleChoice && !currentSelectedOption) ||
              (isFillInBlank && !currentTextAnswer.trim()) ||
              (isMatching && (!currentQ.matchingTerms?.length || currentQ.matchingTerms.some((term) => !matchingPairs[term])))
            }
            className="flex-1 py-3 bg-stone-900 text-white font-bold text-xs rounded-xl notebook-btn hover:bg-stone-800 disabled:opacity-40 transition cursor-pointer flex items-center justify-center gap-2"
          >
            {isSubmitting ? (
              <LoadingOutlined />
            ) : currentIndex + 1 < questions.length ? (
              t('quiz.nextQuestion')
            ) : (
              t('quiz.submitQuiz')
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
