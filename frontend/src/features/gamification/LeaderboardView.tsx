import { useEffect, useState } from 'react';
import { FireOutlined, TrophyOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import { statisticsApi } from '../../services/apiClient';
import { useTranslation } from '../../i18n';
import type { LeaderboardEntry, StudyTarget, UserProfile } from '../../types';

interface LeaderboardViewProps {
  user?: UserProfile | null;
  target?: StudyTarget | null;
  onBack?: () => void;
}

export function LeaderboardView({ user, onBack }: LeaderboardViewProps) {
  const { language } = useTranslation();
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let active = true;
    statisticsApi.getLeaderboard()
      .then((result) => { if (active) setEntries(result); })
      .catch(() => { if (active) setError(true); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);

  const isVi = language === 'vi';
  return (
    <div className="space-y-4 max-w-5xl mx-auto pb-12">
      <div className="bg-white border-notebook-2 rounded-2xl p-5 flex items-center gap-3">
        {onBack && <button type="button" onClick={onBack} className="p-2 rounded-xl border border-stone-200" aria-label={isVi ? 'Quay lại' : 'Back'}><ArrowLeftOutlined /></button>}
        <TrophyOutlined className="text-amber-700 text-xl" />
        <div>
          <h1 className="text-xl font-bold text-stone-900">{isVi ? 'Bảng xếp hạng tuần' : 'Weekly leaderboard'}</h1>
          <p className="text-xs text-stone-600">{isVi ? '10 XP mỗi lượt ôn, 30 XP mỗi bài hoàn thành.' : '10 XP per review, 30 XP per completed lesson.'}</p>
        </div>
      </div>
      <div className="bg-white border-notebook-2 rounded-2xl overflow-hidden">
        {loading && <p className="p-5 text-stone-600">{isVi ? 'Đang tải...' : 'Loading...'}</p>}
        {error && <p className="p-5 text-red-700">{isVi ? 'Không tải được bảng xếp hạng.' : 'Could not load the leaderboard.'}</p>}
        {!loading && !error && entries.length === 0 && <p className="p-5 text-stone-600">{isVi ? 'Chưa có hoạt động học trong tuần này.' : 'No learning activity this week.'}</p>}
        {entries.map((entry, index) => (
          <div key={entry.userId} className={`flex items-center justify-between gap-4 px-5 py-3 border-b border-stone-100 ${entry.userId === user?.id ? 'bg-amber-50' : ''}`}>
            <div className="flex items-center gap-3 min-w-0">
              <span className="w-8 font-bold text-amber-800">#{index + 1}</span>
              <span className="font-semibold truncate">{entry.displayName}</span>
            </div>
            <div className="flex items-center gap-4 text-sm whitespace-nowrap">
              <span>{entry.reviewCount} {isVi ? 'ôn' : 'reviews'}</span>
              <span>{entry.lessonsCompleted} {isVi ? 'bài' : 'lessons'}</span>
              <span className="font-bold text-amber-800">{entry.xp} XP</span>
              {entry.currentStreak > 0 && <span><FireOutlined /> {entry.currentStreak}</span>}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
