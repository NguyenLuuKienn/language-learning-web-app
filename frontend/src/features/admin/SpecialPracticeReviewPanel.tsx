import { useEffect, useState } from 'react';
import { getErrorMessage, specialPracticeApi, type SpecialPracticeReviewDetail, type SpecialPracticeReviewItem } from '../../services/apiClient';
import { useTranslation } from '../../i18n';

interface Props { onClose: () => void }

export function SpecialPracticeReviewPanel({ onClose }: Props) {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const [items, setItems] = useState<SpecialPracticeReviewItem[]>([]);
  const [selected, setSelected] = useState<SpecialPracticeReviewDetail | null>(null);
  const [scores, setScores] = useState<Record<string, string>>({});
  const [feedback, setFeedback] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    specialPracticeApi.pendingReviews().then(setItems)
      .catch((cause) => setError(getErrorMessage(cause, vi ? 'Không tải được bài chờ chấm.' : 'Could not load pending attempts.')));
  }, [vi]);

  const open = async (attemptId: string) => {
    setError('');
    try { setSelected(await specialPracticeApi.review(attemptId)); setScores({}); setFeedback(''); }
    catch (cause) { setError(getErrorMessage(cause, vi ? 'Không mở được bài.' : 'Could not open the attempt.')); }
  };

  const grade = async () => {
    if (!selected || saving) return;
    const subjective = selected.questions.filter((question) => question.options.length === 0);
    if (!feedback.trim() || subjective.some((question) => scores[question.id] === undefined ||
      scores[question.id].trim() === '' || !Number.isInteger(Number(scores[question.id])) ||
      Number(scores[question.id]) < 0 || Number(scores[question.id]) > question.points)) {
      setError(vi ? 'Chấm đủ từng câu và nhập nhận xét.' : 'Score every response and enter feedback.'); return;
    }
    setSaving(true); setError('');
    try {
      await specialPracticeApi.grade(selected.attemptId,
        Object.fromEntries(subjective.map((question) => [question.id, Number(scores[question.id])])), feedback.trim());
      setItems((current) => current.filter((item) => item.attemptId !== selected.attemptId));
      setSelected(null);
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không lưu được điểm.' : 'Could not save the grade.')); }
    finally { setSaving(false); }
  };

  return <div className="fixed inset-0 z-[70] flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true" aria-label={vi ? 'Chấm bài luyện' : 'Review practice'}>
    <section className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-2xl border-notebook-2 bg-white p-5">
      <div className="flex justify-between gap-3"><h2 className="text-lg font-bold">{vi ? 'Chấm luyện thi & phỏng vấn' : 'Review exams & interviews'}</h2>
        <button type="button" onClick={onClose} className="rounded-lg border-notebook px-3 py-2 text-xs">{vi ? 'Đóng' : 'Close'}</button></div>
      {error && <p role="alert" className="mt-3 rounded-lg bg-rose-50 p-3 text-sm text-rose-800">{error}</p>}
      {!selected && <div className="mt-4 space-y-2">{items.length === 0 ? <p className="text-sm text-stone-600">{vi ? 'Không có bài chờ chấm.' : 'No pending attempts.'}</p>
        : items.map((item) => <button type="button" key={item.attemptId} onClick={() => void open(item.attemptId)} className="block w-full rounded-lg border-notebook p-3 text-left text-sm hover:bg-amber-50">
          <strong>{item.title}</strong><span className="ml-2 text-xs text-stone-500">{item.kind} · {item.userId} · {new Date(item.submittedAt).toLocaleString()}</span>
        </button>)}</div>}
      {selected && <div className="mt-4 space-y-4">
        <button type="button" onClick={() => setSelected(null)} className="text-xs underline">{vi ? 'Về danh sách' : 'Back to list'}</button>
        <h3 className="font-bold">{selected.title}</h3>
        {selected.rubric && <p className="rounded-lg bg-amber-50 p-3 text-sm">{vi ? 'Tiêu chí' : 'Rubric'}: {selected.rubric}</p>}
        {selected.objectiveMaxScore > 0 && <p className="text-sm">{vi ? 'Điểm câu khách quan' : 'Objective score'}: {selected.objectiveScore}/{selected.objectiveMaxScore}</p>}
        {selected.questions.filter((question) => question.options.length === 0).map((question) => <div key={question.id} className="rounded-lg border-notebook p-4">
          <p className="text-xs font-bold text-stone-500">{question.skill}</p><h4 className="mt-1 font-bold">{question.prompt}</h4>
          {question.rubric && <p className="mt-2 text-xs text-stone-600">{vi ? 'Tiêu chí' : 'Rubric'}: {question.rubric}</p>}
          <p className="mt-3 whitespace-pre-wrap rounded-lg bg-[#FAF6EE] p-3 text-sm">{selected.answers[question.id]?.text || '—'}</p>
          {selected.answers[question.id]?.audioUrl && <audio controls src={selected.answers[question.id].audioUrl!} className="mt-2 w-full" aria-label={vi ? 'Bản ghi âm của người học' : 'Learner recording'} />}
          <label className="mt-3 block text-xs font-bold">{vi ? 'Điểm' : 'Score'} (0–{question.points})
            <input type="number" min={0} max={question.points} step={1} value={scores[question.id] ?? ''}
              onChange={(event) => setScores((current) => ({ ...current, [question.id]: event.target.value }))}
              className="ml-2 w-24 rounded-lg border-notebook p-2 text-sm" /></label>
        </div>)}
        <label className="block text-xs font-bold">{vi ? 'Nhận xét' : 'Feedback'}
          <textarea rows={4} maxLength={5000} value={feedback} onChange={(event) => setFeedback(event.target.value)} className="mt-2 w-full rounded-lg border-notebook p-3 text-sm" /></label>
        <button type="button" disabled={saving} onClick={() => void grade()} className="rounded-lg bg-stone-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-50">{vi ? 'Lưu đánh giá' : 'Save review'}</button>
      </div>}
    </section>
  </div>;
}
