import React, { useState, useEffect, useRef } from 'react';
import { App } from 'antd';
import { useTranslation } from '../../i18n';
import { getErrorMessage, placementApi } from '../../services/apiClient';
import type { ExamQuestion, ExamResult } from '../../services/examStore';
import { speechService } from '../../services/speechService';
import {
  ClockCircleOutlined,
  SoundOutlined,
  TrophyOutlined,
  SendOutlined,
  ArrowRightOutlined,
  ArrowLeftOutlined,
  CloseOutlined,
  AuditOutlined
} from '@ant-design/icons';

interface ExamRunnerModalProps {
  isOpen: boolean;
  onClose: () => void;
  languageCode: string; // 'en', 'ja', 'zh', 'ko'...
  courseId: string;
  targetLevel: string;  // Level thi vượt cấp hoặc test (ví dụ 'A2', 'B1', 'N4'...)
  isPlacementTest?: boolean;
  onPassLevel: (newLevel: string, placementProof: string) => void | Promise<void>;
}

export const ExamRunnerModal: React.FC<ExamRunnerModalProps> = ({
  isOpen,
  onClose,
  languageCode,
  courseId,
  targetLevel,
  isPlacementTest = false,
  onPassLevel
}) => {
  const { message, modal } = App.useApp();
  const { language, t } = useTranslation();
  const isVi = language === 'vi';
  const sectionName = (section: ExamQuestion['section']) => ({
    listening: isVi ? '🎧 Nghe hiểu' : '🎧 Listening',
    speaking: isVi ? 'Nói / Phát âm' : 'Speaking / Pronunciation',
    grammar_vocab: isVi ? '📖 Ngữ pháp & Từ vựng' : '📖 Grammar & Vocabulary',
    reading: isVi ? '📑 Đọc hiểu' : '📑 Reading',
    writing: isVi ? '✍️ Luyện viết' : '✍️ Writing'
  })[section];

  const [questions, setQuestions] = useState<ExamQuestion[]>([]);
  const [loadingQuestions, setLoadingQuestions] = useState(false);
  const [questionError, setQuestionError] = useState<string | null>(null);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [selectedOptions, setSelectedOptions] = useState<Record<string, number>>({});
  const [writtenAnswers, setWrittenAnswers] = useState<Record<string, string>>({});
  const [audioFileNames, setAudioFileNames] = useState<Record<string, string>>({});
  const [recordingQuestionId, setRecordingQuestionId] = useState<string | null>(null);
  const [uploadingQuestionId, setUploadingQuestionId] = useState<string | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const uploadRecordingRef = useRef(false);
  const openRef = useRef(isOpen);
  const [timeLeftSeconds, setTimeLeftSeconds] = useState(1200); // 20 minutes
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [examResult, setExamResult] = useState<ExamResult | null>(null);
  const [audioSpeed, setAudioSpeed] = useState<number>(1.0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const submitRef = useRef<() => void>(() => {});
  const submittedRef = useRef(false);

  useEffect(() => {
    let active = true;
    openRef.current = isOpen;
    if (isOpen) {
      setQuestions([]);
      setQuestionError(null);
      setLoadingQuestions(true);
      void placementApi.getExam(courseId).then((exam) => {
        if (!active) return;
        setQuestions(exam.questions.map((question) => ({
          ...question,
          languageCode: exam.languageCode,
          level: exam.level,
          explanation: ''
        })));
      }).catch((error: unknown) => {
        if (!active) return;
        const detail = getErrorMessage(error, t('studyTarget.loadTestError'));
        setQuestionError(detail);
        message.error(detail);
      }).finally(() => { if (active) setLoadingQuestions(false); });
      void placementApi.getLatestResult(courseId).then((latest) => {
        if (!active) return;
        if (latest && latest.status !== 'Failed') {
          setExamResult({ ...latest, examId: `result-${latest.attemptId}`, sectionBreakdown: {
            listening: latest.sectionBreakdown.listening,
            speaking: latest.sectionBreakdown.speaking,
            grammar_vocab: { earned: 0, total: 0 },
            reading: latest.sectionBreakdown.reading,
            writing: latest.sectionBreakdown.writing
          } });
          setIsSubmitted(true);
        }
      }).catch(() => {});
      setCurrentIndex(0);
      setSelectedOptions({});
      setWrittenAnswers({});
      setAudioFileNames({});
      setTimeLeftSeconds(1200);
      setIsSubmitted(false);
      submittedRef.current = false;
      setExamResult(null);
    }
    return () => {
      active = false;
      openRef.current = false;
      uploadRecordingRef.current = false;
      if (recorderRef.current?.state === 'recording') recorderRef.current.stop();
      streamRef.current?.getTracks().forEach((track) => track.stop());
    };
  }, [isOpen, courseId, languageCode, targetLevel, t, message]);

  // Timer countdown
  useEffect(() => {
    if (!isOpen || isSubmitted || loadingQuestions || questions.length === 0) return;
    const timer = setInterval(() => {
      setTimeLeftSeconds((prev) => {
        return Math.max(0, prev - 1);
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [isOpen, isSubmitted, loadingQuestions, questions.length]);

  useEffect(() => {
    if (isOpen && timeLeftSeconds === 0 && !isSubmitted) submitRef.current();
  }, [isOpen, isSubmitted, timeLeftSeconds]);

  const currentQ = questions[currentIndex];

  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const handlePlayAudio = (text?: string) => {
    if (!text) return;
    speechService.speak(text, languageCode, {
      rate: audioSpeed
    });
  };

  const handleSelectOption = (qId: string, optionIndex: number) => {
    if (isSubmitted) return;
    setSelectedOptions((prev) => ({ ...prev, [qId]: optionIndex }));
  };

  const handleWritingChange = (qId: string, text: string) => {
    if (isSubmitted) return;
    setWrittenAnswers((prev) => ({ ...prev, [qId]: text }));
  };

  const handleRecordSpeech = async (qId: string) => {
    if (recordingQuestionId === qId) {
      recorderRef.current?.stop();
      setRecordingQuestionId(null);
      return;
    }
    if (recordingQuestionId || !navigator.mediaDevices?.getUserMedia || !window.MediaRecorder?.isTypeSupported('audio/webm')) {
      message.error(isVi ? 'Trình duyệt chưa hỗ trợ ghi âm WebM hoặc đang ghi câu khác.' : 'WebM recording is unavailable or another recording is active.');
      return;
    }
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!openRef.current) { stream.getTracks().forEach((track) => track.stop()); return; }
      streamRef.current = stream;
      uploadRecordingRef.current = true;
      const recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
      recorderRef.current = recorder;
      const chunks: BlobPart[] = [];
      recorder.ondataavailable = (event) => { if (event.data.size) chunks.push(event.data); };
      recorder.onstop = async () => {
        stream.getTracks().forEach((track) => track.stop());
        setRecordingQuestionId(null);
        if (!uploadRecordingRef.current) return;
        setUploadingQuestionId(qId);
        try {
          const uploaded = await placementApi.uploadAudio(courseId, qId, new Blob(chunks, { type: 'audio/webm' }));
          setAudioFileNames((current) => ({ ...current, [qId]: uploaded.fileName }));
          message.success(isVi ? 'Đã lưu bản ghi âm.' : 'Recording saved.');
        } catch {
          message.error(isVi ? 'Không tải được bản ghi âm. Hãy ghi lại.' : 'Recording upload failed. Please retry.');
        } finally { setUploadingQuestionId(null); }
      };
      recorder.start();
      setRecordingQuestionId(qId);
    } catch {
      message.error(isVi ? 'Không thể dùng micro. Hãy cấp quyền truy cập.' : 'Microphone access is required.');
    }
  };

  const handleSubmitExam = async () => {
    if (submittedRef.current) return;
    if (questions.some((question) => question.section === 'speaking' && !audioFileNames[question.id]) ||
        questions.some((question) => question.section === 'writing' && !writtenAnswers[question.id]?.trim())) {
      message.warning(isVi ? 'Hãy ghi âm phần nói và hoàn thành phần viết trước khi nộp.' : 'Record speaking answers and complete writing before submitting.');
      return;
    }
    submittedRef.current = true;
    setIsSubmitting(true);
    try {
      const result = await placementApi.submit(courseId, selectedOptions, writtenAnswers, audioFileNames);
      const res: ExamResult = {
        ...result,
        examId: `result-${Date.now()}`,
        sectionBreakdown: {
          listening: result.sectionBreakdown.listening,
          speaking: result.sectionBreakdown.speaking,
          grammar_vocab: { earned: 0, total: 0 },
          reading: result.sectionBreakdown.reading,
          writing: result.sectionBreakdown.writing
        }
      };
      setExamResult(res);
      setIsSubmitted(true);
      if (result.passed && result.placementProof) {
        message.success(t('placementExam.passed', { score: result.percentage, level: targetLevel }));
        await onPassLevel(targetLevel, result.placementProof);
      } else if (result.status === 'Pending') {
        message.info(isVi ? 'Bài nói và bài viết đang chờ quản trị viên đánh giá.' : 'Speaking and writing are awaiting review.');
      } else {
        message.warning(t('placementExam.failed', { score: result.percentage }));
      }
    } catch {
      submittedRef.current = false;
      message.error(t('common.error'));
    } finally {
      setIsSubmitting(false);
    }
  };
  const refreshResult = async () => {
    if (!examResult?.attemptId) return;
    try {
      const result = await placementApi.getResult(examResult.attemptId);
      setExamResult({ ...examResult, ...result, sectionBreakdown: {
        listening: result.sectionBreakdown.listening,
        speaking: result.sectionBreakdown.speaking,
        grammar_vocab: { earned: 0, total: 0 },
        reading: result.sectionBreakdown.reading,
        writing: result.sectionBreakdown.writing
      } });
      if (result.passed && result.placementProof) await onPassLevel(targetLevel, result.placementProof);
      else if (result.status === 'Pending') message.info(isVi ? 'Bài làm vẫn đang chờ đánh giá.' : 'Your submission is still under review.');
    } catch { message.error(t('common.error')); }
  };
  useEffect(() => { submitRef.current = handleSubmitExam; });

  const confirmSubmit = () => {
    const answeredCount =
      Object.keys(selectedOptions).length +
      Object.keys(writtenAnswers).filter((k) => writtenAnswers[k]?.trim().length > 0).length +
      Object.keys(audioFileNames).length;

    modal.confirm({
      title: isVi ? 'Xác nhận nộp bài thi?' : 'Submit your exam?',
      content: isVi
        ? `Bạn đã hoàn thành ${answeredCount}/${questions.length} câu. Bạn có chắc chắn muốn nộp bài ngay bây giờ?`
        : `You completed ${answeredCount}/${questions.length} questions. Are you sure you want to submit?`,
      okText: isVi ? 'Nộp bài ngay' : 'Submit Now',
      cancelText: isVi ? 'Làm tiếp' : 'Continue',
      onOk: () => handleSubmitExam()
    });
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 md:p-6 bg-black/70 backdrop-blur-xs overflow-y-auto">
      <div className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-4xl w-full p-6 my-auto space-y-5">
        {/* Header Phòng thi */}
        <div className="flex flex-wrap items-center justify-between border-b-2 border-[#262626] pb-4 gap-3">
          <div className="flex items-center gap-3">
            <span className="w-10 h-10 rounded-xl bg-amber-400 border border-stone-900 flex items-center justify-center text-xl text-stone-900 shadow-2xs">
              <AuditOutlined />
            </span>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-stone-900 text-white uppercase">
                  {isPlacementTest ? (isVi ? 'BÀI KIỂM TRA ĐẦU VÀO' : 'PLACEMENT ASSESSMENT') : (isVi ? 'BÀI THI VƯỢT CẤP' : 'LEVEL-UP EXAM')}
                </span>
                <span className="text-xs font-mono font-bold text-indigo-700 bg-indigo-50 border border-indigo-200 px-2 py-0.5 rounded">
                  {isVi ? 'MỤC TIÊU' : 'TARGET'}: {targetLevel}
                </span>
              </div>
              <h2 className="text-lg font-black text-stone-900 tracking-tight mt-0.5">
                {isPlacementTest ? (isVi ? 'Đánh giá năng lực xếp lớp' : 'Placement assessment') : (isVi ? `Kỳ thi lên trình độ ${targetLevel}` : `Level-up exam for ${targetLevel}`)}
              </h2>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {/* Countdown Timer */}
            {!isSubmitted && (
              <div
                className={`flex items-center gap-2 px-3 py-1.5 rounded-xl border-2 font-mono text-xs font-bold ${
                  timeLeftSeconds < 180
                    ? 'bg-rose-100 text-rose-800 border-rose-400 animate-pulse'
                    : 'bg-white text-stone-800 border-stone-300'
                }`}
              >
                <ClockCircleOutlined />
                <span>{formatTime(timeLeftSeconds)}</span>
              </div>
            )}

            <button
              onClick={onClose}
              className="p-1.5 rounded-lg border border-stone-300 hover:bg-stone-200 text-stone-600 transition cursor-pointer"
            >
              <CloseOutlined />
            </button>
          </div>
        </div>

        {/* MÀN HÌNH KẾT QUẢ SAU KHI NỘP BÀI */}
        {isSubmitted && examResult ? (
          <div className="space-y-6 py-4">
            {examResult.status === 'Pending' ? <div className="rounded-xl border border-amber-300 bg-amber-50 p-6 text-center">
              <h3 className="text-lg font-bold">{isVi ? 'Đã nộp bài, đang chờ đánh giá' : 'Submitted, awaiting review'}</h3>
              <p className="mt-2 text-sm">{isVi ? 'Quản trị viên sẽ nghe bản ghi âm và chấm bài viết trước khi xác nhận trình độ.' : 'An administrator will review your recording and writing before confirming your level.'}</p>
              <button type="button" onClick={() => { void refreshResult(); }} className="mt-4 rounded bg-stone-900 px-4 py-2 text-white">{isVi ? 'Kiểm tra kết quả' : 'Check result'}</button>
            </div> : <>
            <div
              className={`p-6 rounded-2xl border-2 text-center space-y-3 ${
                examResult.passed
                  ? 'bg-emerald-50 border-emerald-400 text-emerald-950'
                  : 'bg-amber-50 border-amber-300 text-amber-950'
              }`}
            >
              <div className="text-4xl">
                {examResult.passed ? <TrophyOutlined className="text-amber-500" /> : '📚'}
              </div>
              <h3 className="text-xl font-black">
                {examResult.passed
                  ? (isVi ? `Bạn đã đạt trình độ ${targetLevel}!` : `You passed level ${targetLevel}!`)
                  : (isVi ? `Bạn đạt ${examResult.percentage}% — chưa đủ điểm yêu cầu.` : `You scored ${examResult.percentage}% — below the required score.`)}
              </h3>
              <p className="text-xs max-w-lg mx-auto">
                {examResult.passed
                  ? (isVi ? `Bạn có thể tiếp tục học ở trình độ ${targetLevel}.` : `You can continue learning at level ${targetLevel}.`)
                  : (isVi ? 'Hãy ôn lại kiến thức rồi thử lại khi sẵn sàng.' : 'Review the material and try again when you are ready.')}
              </p>

              <div className="inline-flex items-center gap-6 px-6 py-3 bg-white rounded-xl border border-stone-300 shadow-2xs font-mono">
                <div>
                  <div className="text-xs text-stone-500">{isVi ? 'ĐIỂM SỐ' : 'SCORE'}</div>
                  <div className="text-2xl font-black text-stone-900">
                    {examResult.earnedPoints} / {examResult.totalPoints}
                  </div>
                </div>
                <div className="w-[1px] h-8 bg-stone-200" />
                <div>
                  <div className="text-xs text-stone-500">{isVi ? 'TỶ LỆ' : 'PERCENTAGE'}</div>
                  <div className={`text-2xl font-black ${examResult.passed ? 'text-emerald-600' : 'text-amber-600'}`}>
                    {examResult.percentage}%
                  </div>
                </div>
                <div className="w-[1px] h-8 bg-stone-200" />
                <div>
                  <div className="text-xs text-stone-500">{isVi ? 'KẾT QUẢ' : 'RESULT'}</div>
                  <div className={`text-sm font-bold uppercase ${examResult.passed ? 'text-emerald-700' : 'text-rose-600'}`}>
                    {examResult.passed ? (isVi ? 'ĐẠT' : 'PASSED') : (isVi ? 'CHƯA ĐẠT' : 'NOT PASSED')}
                  </div>
                </div>
              </div>
            </div>

            {/* Phân tích điểm từng kỹ năng */}
            <div className="bg-white border-notebook rounded-xl p-4 space-y-3">
              <h4 className="text-xs font-bold text-stone-800 uppercase tracking-wider">
                {isVi ? 'Kết quả từng phần thi' : 'Results by section'}
              </h4>
              <div className="grid grid-cols-2 sm:grid-cols-5 gap-2 text-center text-xs">
                <div className="p-3 bg-stone-50 rounded-lg border border-stone-200">
                  <div className="text-stone-500">{sectionName('listening')}</div>
                  <div className="font-bold text-stone-900 mt-1">
                    {examResult.sectionBreakdown.listening.earned} / {examResult.sectionBreakdown.listening.total} {isVi ? 'đ' : 'pts'}
                  </div>
                </div>
                <div className="p-3 bg-stone-50 rounded-lg border border-stone-200">
                  <div className="text-stone-500">{sectionName('speaking')}</div>
                  <div className="font-bold text-stone-900 mt-1">{examResult.sectionBreakdown.speaking.earned} / {examResult.sectionBreakdown.speaking.total} {isVi ? 'đ' : 'pts'}</div>
                </div>
                <div className="p-3 bg-stone-50 rounded-lg border border-stone-200">
                  <div className="text-stone-500">{sectionName('grammar_vocab')}</div>
                  <div className="font-bold text-stone-900 mt-1">
                    {examResult.sectionBreakdown.grammar_vocab.earned} / {examResult.sectionBreakdown.grammar_vocab.total} {isVi ? 'đ' : 'pts'}
                  </div>
                </div>
                <div className="p-3 bg-stone-50 rounded-lg border border-stone-200">
                  <div className="text-stone-500">{sectionName('reading')}</div>
                  <div className="font-bold text-stone-900 mt-1">
                    {examResult.sectionBreakdown.reading.earned} / {examResult.sectionBreakdown.reading.total} {isVi ? 'đ' : 'pts'}
                  </div>
                </div>
                <div className="p-3 bg-stone-50 rounded-lg border border-stone-200">
                  <div className="text-stone-500">{sectionName('writing')}</div>
                  <div className="font-bold text-stone-900 mt-1">
                    {examResult.sectionBreakdown.writing.earned} / {examResult.sectionBreakdown.writing.total} {isVi ? 'đ' : 'pts'}
                  </div>
                </div>
              </div>
            </div>
            </>}

            {/* Nút đóng */}
            <div className="flex justify-end gap-3 pt-2">
              {examResult.passed && examResult.status === 'Passed' && <button
                type="button"
                onClick={() => { void refreshResult(); }}
                className="px-6 py-2.5 bg-amber-400 text-stone-900 rounded-xl text-xs font-bold"
              >{isVi ? 'Áp dụng trình độ' : 'Apply level'}</button>}
              <button
                type="button"
                onClick={onClose}
                className="px-6 py-2.5 bg-stone-900 text-white rounded-xl text-xs font-bold notebook-btn cursor-pointer"
              >
                {isVi ? 'Hoàn tất & trở về' : 'Finish & return'}
              </button>
            </div>
          </div>
        ) : (
          /* MÀN HÌNH LÀM BÀI THI */
          <div className="space-y-5">
            {/* Thanh Palette các câu hỏi (1..N) */}
            <div className="flex items-center gap-1.5 overflow-x-auto pb-2 border-b border-stone-200">
              {questions.map((q, idx) => {
                const isCurrent = idx === currentIndex;
                const isAnswered =
                  q.section === 'writing' || q.section === 'speaking'
                    ? q.section === 'speaking' ? !!audioFileNames[q.id] : !!writtenAnswers[q.id]?.trim()
                    : selectedOptions[q.id] !== undefined;

                return (
                  <button
                    key={q.id}
                    onClick={() => setCurrentIndex(idx)}
                    className={`w-8 h-8 rounded-lg text-xs font-mono font-bold flex items-center justify-center border transition cursor-pointer shrink-0 ${
                      isCurrent
                        ? 'bg-amber-400 text-stone-900 border-stone-900 ring-2 ring-amber-300'
                        : isAnswered
                        ? 'bg-emerald-100 text-emerald-800 border-emerald-400'
                        : 'bg-white text-stone-600 border-stone-300 hover:bg-stone-100'
                    }`}
                  >
                    {idx + 1}
                  </button>
                );
              })}
            </div>

            {/* Khung nội dung câu hỏi hiện tại */}
            {loadingQuestions && <p className="rounded-xl bg-white p-5">{isVi ? 'Đang tải đề thi...' : 'Loading test...'}</p>}
            {!loadingQuestions && questions.length === 0 && <p className="p-5 text-stone-700 bg-white rounded-xl">{questionError ?? (isVi ? 'Chưa có bài đánh giá phù hợp. Bạn có thể quay lại sau.' : 'No matching assessment is available yet. Please return later.')}</p>}
            {currentQ ? (
              <div className="bg-white border-notebook-2 rounded-2xl p-5 space-y-4 shadow-2xs">
                <div className="flex items-center justify-between border-b border-stone-200 pb-2">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-mono font-bold px-2 py-0.5 rounded bg-stone-100 text-stone-700 uppercase">
                       {isVi ? 'Câu' : 'Question'} {currentIndex + 1} / {questions.length}
                    </span>
                    <span className="text-xs font-bold text-amber-700 bg-amber-50 px-2 py-0.5 rounded border border-amber-200 uppercase">
                       {sectionName(currentQ.section)}
                    </span>
                  </div>
                  <span className="text-xs font-mono font-bold text-stone-500">
                     {currentQ.points} {isVi ? 'điểm' : 'points'}
                  </span>
                </div>

                {/* Yêu cầu của câu hỏi */}
                <h3 className="text-sm font-bold text-stone-900 leading-snug">
                  {currentQ.prompt}
                </h3>

                {/* PHẦN NGHE: Bấm nghe Audio TTS */}
                {currentQ.section === 'listening' && currentQ.audioScript && (
                  <div className="p-4 bg-amber-50/60 border border-amber-200 rounded-xl flex items-center justify-between gap-3">
                    <div className="flex items-center gap-3">
                      <button
                        type="button"
                        onClick={() => handlePlayAudio(currentQ.audioScript)}
                        className="px-4 py-2 bg-amber-400 hover:bg-amber-500 text-stone-900 font-bold text-xs rounded-xl notebook-btn flex items-center gap-2 cursor-pointer shadow-2xs"
                      >
                        <SoundOutlined className="text-base" />
                         <span>{isVi ? 'Nghe đoạn hội thoại' : 'Play the conversation'}</span>
                      </button>
                      <span className="text-xs text-stone-500 italic">
                         {isVi ? 'Nghe rồi chọn câu trả lời bên dưới.' : 'Listen and choose an answer below.'}
                      </span>
                    </div>

                    <div className="flex items-center gap-1.5 text-xs font-mono">
                       <span className="text-stone-500">{isVi ? 'Tốc độ:' : 'Speed:'}</span>
                      {[0.8, 1.0, 1.2].map((sp) => (
                        <button
                          key={sp}
                          type="button"
                          onClick={() => setAudioSpeed(sp)}
                          className={`px-1.5 py-0.5 rounded text-[11px] font-bold cursor-pointer ${
                            audioSpeed === sp ? 'bg-stone-900 text-white' : 'bg-white border text-stone-700'
                          }`}
                        >
                          {sp}x
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                {/* PHẦN ĐỌC HIỂU: Hiển thị đoạn văn bản */}
                {currentQ.section === 'reading' && currentQ.readingPassage && (
                  <div className="p-4 bg-stone-50 border border-stone-200 rounded-xl text-xs text-stone-800 leading-relaxed font-serif">
                    {currentQ.readingPassage}
                  </div>
                )}

                {/* LỰA CHỌN TRẮC NGHIỆM (Listening, Grammar, Reading) */}
                {currentQ.options && currentQ.options.length > 0 && (
                  <div className="space-y-2 pt-2">
                    {currentQ.options.map((opt, optIdx) => {
                      const isChosen = selectedOptions[currentQ.id] === optIdx;
                      return (
                        <button
                          key={optIdx}
                          type="button"
                          onClick={() => handleSelectOption(currentQ.id, optIdx)}
                          className={`w-full p-3 rounded-xl border-2 text-left text-xs transition flex items-center gap-3 cursor-pointer ${
                            isChosen
                              ? 'bg-amber-50 border-stone-900 shadow-xs ring-1 ring-amber-300 font-bold text-stone-900'
                              : 'bg-white border-stone-300 hover:border-stone-400 text-stone-700'
                          }`}
                        >
                          <span
                            className={`w-6 h-6 rounded-md font-mono text-xs flex items-center justify-center font-bold border ${
                              isChosen
                                ? 'bg-stone-900 text-white border-stone-900'
                                : 'bg-stone-100 text-stone-600 border-stone-300'
                            }`}
                          >
                            {String.fromCharCode(65 + optIdx)}
                          </span>
                          <span className="flex-1">{opt}</span>
                        </button>
                      );
                    })}
                  </div>
                )}

                {/* PHẦN VIẾT: Ô nhập văn bản */}
                {currentQ.section === 'speaking' && (
                  <div className="space-y-2 pt-2">
                    <button type="button" disabled={!!uploadingQuestionId || (!!recordingQuestionId && recordingQuestionId !== currentQ.id)} onClick={() => { void handleRecordSpeech(currentQ.id); }} className="px-4 py-2 bg-amber-400 rounded-lg font-bold text-xs disabled:opacity-50">
                      {recordingQuestionId === currentQ.id ? (isVi ? 'Dừng ghi âm' : 'Stop recording') : (isVi ? 'Bắt đầu ghi âm' : 'Start recording')}
                    </button>
                    <p className="text-xs text-stone-600">{uploadingQuestionId === currentQ.id ? (isVi ? 'Đang tải ghi âm...' : 'Uploading recording...') : audioFileNames[currentQ.id] ? (isVi ? 'Đã lưu ghi âm. Bạn có thể ghi lại.' : 'Recording saved. You can record again.') : (isVi ? 'Ghi âm câu trả lời để quản trị viên nghe và đánh giá.' : 'Record your answer for review.')}</p>
                  </div>
                )}

                {currentQ.section === 'writing' && (
                  <div className="space-y-2 pt-2">
                    <textarea
                      rows={5}
                      value={writtenAnswers[currentQ.id] || ''}
                      onChange={(e) => handleWritingChange(currentQ.id, e.target.value)}
                       placeholder={isVi ? 'Viết câu trả lời bằng ngôn ngữ đang học...' : 'Write your answer in the language you are learning...'}
                      className="w-full p-3 bg-white border border-stone-300 rounded-xl text-xs outline-none focus:border-stone-900 leading-relaxed"
                    />
                    <div className="flex items-center justify-between text-[11px] text-stone-500 font-mono">
                      <span>
                         {isVi ? 'Số từ đã viết:' : 'Words written:'}{' '}
                        <strong className="text-stone-900">
                          {(writtenAnswers[currentQ.id] || '').trim().split(/\s+/).filter(Boolean).length}
                        </strong>{' '}
                         / {isVi ? 'tối thiểu' : 'minimum'} {currentQ.minWords || 10} {isVi ? 'từ' : 'words'}
                      </span>
                      <span>{isVi ? 'Đối chiếu với đáp án và độ dài tối thiểu' : 'Compared with the expected response and minimum length'}</span>
                    </div>
                  </div>
                )}
              </div>
            ) : null}

            {/* Thanh điều hướng Trước / Sau & Nộp bài */}
            <div className="flex items-center justify-between pt-2">
              <button
                type="button"
                disabled={currentIndex === 0}
                onClick={() => setCurrentIndex((prev) => Math.max(0, prev - 1))}
                className="px-4 py-2 rounded-xl border border-stone-300 bg-white hover:bg-stone-100 text-xs font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-40"
              >
                <ArrowLeftOutlined />
                 <span>{isVi ? 'Câu trước' : 'Previous question'}</span>
              </button>

              <div className="flex items-center gap-2">
                {currentIndex < questions.length - 1 ? (
                  <button
                    type="button"
                    onClick={() => setCurrentIndex((prev) => Math.min(questions.length - 1, prev + 1))}
                    className="px-4 py-2 rounded-xl bg-stone-900 text-white hover:bg-stone-800 text-xs font-bold notebook-btn flex items-center gap-1.5 cursor-pointer"
                  >
                     <span>{isVi ? 'Câu tiếp' : 'Next question'}</span>
                    <ArrowRightOutlined />
                  </button>
                ) : null}

                <button
                  type="button"
                  onClick={confirmSubmit}
                  disabled={questions.length === 0 || isSubmitting || !!recordingQuestionId || !!uploadingQuestionId}
                  className="px-5 py-2 rounded-xl bg-amber-400 hover:bg-amber-500 text-stone-900 text-xs font-black notebook-btn flex items-center gap-2 cursor-pointer shadow-xs"
                >
                  <SendOutlined />
                   <span>{isVi ? 'Nộp bài' : 'Submit assessment'}</span>
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
