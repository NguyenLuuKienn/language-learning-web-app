import { useEffect, useState } from 'react';
import { App } from 'antd';
import { useTranslation } from '../../i18n';
import { getErrorMessage, placementApi, type PlacementReview } from '../../services/apiClient';

export function PlacementReviewPanel({ onClose }: { onClose: () => void }) {
  const { message } = App.useApp();
  const { language } = useTranslation();
  const isVi = language === 'vi';
  const [items, setItems] = useState<PlacementReview[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [scores, setScores] = useState<Record<string, number>>({});
  const [feedback, setFeedback] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let active = true;
    placementApi.getPendingReviews().then((data) => {
      if (active) setItems(data);
    }).catch((error) => {
      if (active) message.error(getErrorMessage(error, 'Không tải được bài cần chấm.'));
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [message]);

  const selected = items.find((item) => item.attemptId === selectedId) ?? items[0];
  const subjective = selected?.questions.filter((question) => question.section === 'speaking' || question.section === 'writing') ?? [];

  const submit = async () => {
    if (!selected) return;
    if (subjective.some((question) => !Number.isInteger(scores[question.id]) || scores[question.id] < 0 || scores[question.id] > question.points)) {
      message.error(isVi ? 'Chấm điểm từng câu nói và viết trong giới hạn điểm.' : 'Score every speaking and writing answer within its point range.');
      return;
    }
    setSaving(true);
    try {
      await placementApi.review(selected.attemptId, scores, feedback);
      setItems((current) => current.filter((item) => item.attemptId !== selected.attemptId));
      setSelectedId(null);
      setScores({});
      setFeedback('');
      message.success(isVi ? 'Đã lưu kết quả đánh giá.' : 'Review saved.');
    } catch (error) {
      message.error(getErrorMessage(error, isVi ? 'Không lưu được kết quả.' : 'Could not save review.'));
    } finally { setSaving(false); }
  };

  return <div className="fixed inset-0 z-[60] overflow-y-auto bg-black/60 p-4" role="dialog" aria-modal="true" aria-label="Placement reviews">
    <div className="mx-auto my-8 max-w-5xl rounded-2xl border-2 border-stone-900 bg-[#FAF6EE] p-5">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold">{isVi ? 'Đánh giá bài đầu vào' : 'Placement reviews'} ({items.length})</h2>
        <button type="button" onClick={onClose} className="rounded border border-stone-900 px-3 py-2">{isVi ? 'Đóng' : 'Close'}</button>
      </div>
      {loading ? <p>{isVi ? 'Đang tải...' : 'Loading...'}</p> : items.length === 0 ? <p>{isVi ? 'Không có bài đang chờ.' : 'No pending reviews.'}</p> : <div className="grid gap-4 md:grid-cols-[250px_1fr]">
        <nav className="max-h-[65vh] overflow-y-auto rounded border border-stone-300 bg-white p-2" aria-label="Pending attempts">
          {items.map((item) => <button key={item.attemptId} type="button" onClick={() => { setSelectedId(item.attemptId); setScores({}); setFeedback(''); }}
            className={`mb-1 w-full rounded p-2 text-left text-xs ${selected?.attemptId === item.attemptId ? 'bg-amber-200' : 'hover:bg-stone-100'}`}>
            <span className="block font-semibold">{item.learnerName}</span><span className="block truncate">{item.courseTitle}</span><span>{new Date(item.submittedAt).toLocaleString(isVi ? 'vi-VN' : 'en-US')}</span>
          </button>)}
        </nav>
        <div className="max-h-[65vh] space-y-3 overflow-y-auto pr-2">
          {selected && <><p className="text-xs text-stone-600">{isVi ? 'Khóa học' : 'Course'}: {selected.courseTitle} · {selected.learnerName}</p>
            {subjective.map((question) => <section key={question.id} className="rounded-xl border border-stone-300 bg-white p-4">
              <div className="mb-2 flex items-center justify-between"><h3 className="font-bold capitalize">{question.section}</h3><span>{question.points} {isVi ? 'điểm' : 'points'}</span></div>
              <p className="mb-2 font-semibold">{question.prompt}</p>
              {question.rubric && <p className="mb-2 text-sm text-stone-600">Rubric: {question.rubric}</p>}
              {question.expectedResponse && <p className="mb-2 text-sm text-stone-600">{isVi ? 'Bài mẫu' : 'Expected response'}: {question.expectedResponse}</p>}
              {question.section === 'speaking' && question.audioUrl && <audio controls preload="none" src={question.audioUrl} className="mb-2 w-full" />}
              {question.section === 'writing' && <p className="mb-2 whitespace-pre-wrap rounded bg-stone-50 p-3 text-sm">{question.writtenAnswer}</p>}
              <label className="text-sm">{isVi ? 'Điểm' : 'Score'} <input type="number" min={0} max={question.points} value={scores[question.id] ?? ''}
                onChange={(event) => setScores((current) => ({ ...current, [question.id]: Number(event.target.value) }))}
                className="ml-2 w-20 rounded border border-stone-300 p-1" /></label>
            </section>)}
            <label className="block text-sm">{isVi ? 'Nhận xét' : 'Feedback'}<textarea value={feedback} onChange={(event) => setFeedback(event.target.value)} rows={3} maxLength={4000} className="mt-1 w-full rounded border border-stone-300 bg-white p-2" /></label>
            <button type="button" disabled={saving} onClick={() => { void submit(); }} className="rounded bg-stone-900 px-4 py-2 text-white disabled:opacity-50">{saving ? '...' : isVi ? 'Lưu đánh giá' : 'Save review'}</button>
          </>}
        </div>
      </div>}
    </div>
  </div>;
}
