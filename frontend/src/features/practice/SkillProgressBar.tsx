interface Props {
  completed: number;
  practiced?: number;
  pendingAssessment?: boolean;
  total: number;
  onResume?: () => void;
  hasResume?: boolean;
  language: 'vi' | 'en';
  error?: string;
}

export function SkillProgressBar({ completed, practiced = 0, pendingAssessment = false, total, onResume, hasResume, language, error }: Props) {
  const vi = language === 'vi';
  const progress = pendingAssessment ? practiced : completed;
  return <div className="rounded-xl border border-stone-300 bg-white p-4 text-sm">
    <div className="flex flex-wrap items-center justify-between gap-2"><strong>{vi ? 'Tiến độ luyện tập' : 'Practice progress'}</strong>
      <span>{progress}/{total} {pendingAssessment ? (vi ? 'đã luyện, chờ đánh giá' : 'practiced, awaiting assessment') : (vi ? 'đã hoàn thành' : 'completed')}</span></div>
    <div className="mt-2 h-2 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-emerald-600 transition-all" style={{ width: `${total ? progress / total * 100 : 0}%` }} /></div>
    {error && <p role="alert" className="mt-2 text-xs text-rose-700">{vi ? 'Chưa tải được tiến độ. Vui lòng thử lại sau.' : 'Progress is unavailable. Please try again later.'}</p>}
    {hasResume && onResume && <button type="button" onClick={onResume} className="mt-3 rounded-lg bg-amber-300 px-3 py-2 text-xs font-bold">{vi ? 'Tiếp tục học' : 'Continue learning'}</button>}
  </div>;
}
