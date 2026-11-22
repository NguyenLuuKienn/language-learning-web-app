import { useState } from 'react';
import { adminApi, getErrorMessage } from '../../services/apiClient';
import { useTranslation } from '../../i18n';

type Readiness = Awaited<ReturnType<typeof adminApi.getSrsEvaluationReadiness>>;

export function SrsEvaluationPanel() {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const [report, setReport] = useState<Readiness | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const load = async () => {
    setLoading(true);
    setError('');
    try { setReport(await adminApi.getSrsEvaluationReadiness()); }
    catch (reason) { setError(getErrorMessage(reason, vi ? 'Không tải được dữ liệu ôn.' : 'Could not load review data.')); }
    finally { setLoading(false); }
  };
  return <section className="space-y-3 rounded-2xl border-notebook bg-white p-5">
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div><h2 className="text-lg font-bold">{vi ? 'Dữ liệu đánh giá SRS' : 'SRS evaluation data'}</h2>
        <p className="text-xs text-stone-600">{vi ? 'Số liệu tổng hợp để kiểm tra lịch sử trước khi thử thuật toán mới; chưa phải kết quả so sánh FSRS.' : 'Aggregated history checks before testing a new scheduler; this is not an FSRS comparison.'}</p></div>
      <button type="button" onClick={() => void load()} disabled={loading} className="rounded-lg bg-amber-300 px-4 py-2 text-xs font-bold disabled:opacity-50">{loading ? '...' : vi ? 'Tải số liệu' : 'Load metrics'}</button>
    </div>
    {error && <p role="alert" className="text-sm text-rose-700">{error}</p>}
    {report && <>
      <div className="grid gap-2 text-sm sm:grid-cols-2 lg:grid-cols-4">
        <p>{vi ? 'Lượt ôn' : 'Reviews'}: <strong>{report.totalReviews}</strong></p>
        <p>{vi ? 'Người học' : 'Learners'}: <strong>{report.distinctLearners}</strong></p>
        <p>{vi ? 'Thẻ' : 'Cards'}: <strong>{report.distinctCards}</strong></p>
        <p>{vi ? 'Lượt đến hạn có đủ khoảng cách' : 'Due reviews with interval'}: <strong>{report.dueReviewsWithInterval}</strong></p>
      </div>
      <p className="text-sm">{vi ? 'Nhớ ở lượt đến hạn có đủ dữ liệu' : 'Recall on due reviews with full data'}: <strong>{report.dueReviewsWithInterval ? Math.round(report.dueReviewsRecalled / report.dueReviewsWithInterval * 100) + '%' : '—'}</strong></p>
      <p className="text-xs text-stone-600">{vi ? `Thiếu cờ đến hạn: ${report.missingDueFlag}; thiếu khoảng cách trước đó: ${report.missingPreviousInterval}.` : `Missing due flag: ${report.missingDueFlag}; missing prior interval: ${report.missingPreviousInterval}.`}</p>
      <p className="text-xs text-stone-600">{Object.entries(report.reviewsByAlgorithm).map(([version, count]) => `${version}: ${count}`).join(' · ') || (vi ? 'Chưa có lượt ôn' : 'No reviews yet')}</p>
    </>}
  </section>;
}
