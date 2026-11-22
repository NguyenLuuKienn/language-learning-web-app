import React, { useState, useEffect } from 'react';
import { useTranslation } from '../../i18n';
import { statisticsApi } from '../../services/apiClient';
import type { StatisticsOverview, HeatmapDay } from '../../types';
import {
  FireOutlined,
  TrophyOutlined,
  CheckCircleOutlined,
  FieldTimeOutlined,
  LoadingOutlined
} from '@ant-design/icons';

export const StatisticsView: React.FC = () => {
  const { t } = useTranslation();
  const [overview, setOverview] = useState<StatisticsOverview | null>(null);
  const [heatmap, setHeatmap] = useState<HeatmapDay[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const loadStats = async () => {
    setIsLoading(true);
    setLoadError(false);
    try {
      const [ovData, hmData] = await Promise.all([
        statisticsApi.getOverview(),
        statisticsApi.getHeatmap()
      ]);
      setOverview(ovData);
      setHeatmap(hmData);
    } catch {
      setOverview(null);
      setHeatmap([]);
      setLoadError(true);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadStats();
  }, []);

  const getHeatmapColor = (level: number) => {
    switch (level) {
      case 1:
        return 'bg-emerald-200 border-emerald-300';
      case 2:
        return 'bg-emerald-400 border-emerald-500';
      case 3:
        return 'bg-emerald-600 border-emerald-700';
      case 4:
        return 'bg-emerald-800 border-emerald-900';
      default:
        return 'bg-stone-200/80 border-stone-300/60';
    }
  };

  if (isLoading && !overview) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <LoadingOutlined className="text-3xl text-stone-500 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {loadError && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{t('common.error')} <button type="button" onClick={() => void loadStats()} className="ml-2 underline">{t('quiz.retryQuiz')}</button></div>}
      <div>
        <h1 className="text-xl font-bold text-stone-900">{t('stats.title')}</h1>
        <p className="text-xs text-stone-600">{t('stats.subtitle')}</p>
      </div>

      {overview && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white border-notebook rounded-2xl p-4 shadow-xs relative overflow-hidden">
            <div className="flex items-center justify-between text-amber-500 mb-2">
              <span className="text-xs font-bold text-stone-600 uppercase tracking-wider">
                {t('stats.currentStreak')}
              </span>
              <FireOutlined className="text-xl text-amber-500" />
            </div>
            <div className="text-3xl font-black text-stone-900">
              {overview.currentStreakDays ?? overview.currentStreak ?? 0}{' '}
              <span className="text-xs font-medium text-stone-500">{t('stats.days')}</span>
            </div>
          </div>

          <div className="bg-white border-notebook rounded-2xl p-4 shadow-xs">
            <div className="flex items-center justify-between text-emerald-600 mb-2">
              <span className="text-xs font-bold text-stone-600 uppercase tracking-wider">
                {t('stats.retentionRate')}
              </span>
              <TrophyOutlined className="text-xl" />
            </div>
            <div className="text-3xl font-black text-stone-900">
              {overview.retentionRatePercentage ?? overview.retentionRate ?? 0}%
            </div>
          </div>

          <div className="bg-white border-notebook rounded-2xl p-4 shadow-xs">
            <div className="flex items-center justify-between text-sky-600 mb-2">
              <span className="text-xs font-bold text-stone-600 uppercase tracking-wider">
                {t('stats.mastered')}
              </span>
              <CheckCircleOutlined className="text-xl" />
            </div>
            <div className="text-3xl font-black text-stone-900">
              {overview.cardsMastered ?? 0}
            </div>
          </div>

          <div className="bg-white border-notebook rounded-2xl p-4 shadow-xs">
            <div className="flex items-center justify-between text-rose-600 mb-2">
              <span className="text-xs font-bold text-stone-600 uppercase tracking-wider">
                {t('stats.todayGoal')}
              </span>
              <FieldTimeOutlined className="text-xl" />
            </div>
            <div className="text-3xl font-black text-stone-900">
              {overview.todayReviewsCount ?? 0} / {overview.dailyGoal ?? overview.dailyTarget ?? 10}
            </div>
          </div>
        </div>
      )}

      <div className="bg-white border-notebook-2 rounded-2xl p-6 shadow-xs space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="font-bold text-sm text-stone-900">{t('stats.heatmapTitle')}</h2>
          <div className="flex items-center gap-1.5 text-[10px] text-stone-500 font-mono">
            <span>{t('stats.less')}</span>
            <div className="w-2.5 h-2.5 rounded-xs bg-stone-200 border border-stone-300" />
            <div className="w-2.5 h-2.5 rounded-xs bg-emerald-200 border border-emerald-300" />
            <div className="w-2.5 h-2.5 rounded-xs bg-emerald-400 border border-emerald-500" />
            <div className="w-2.5 h-2.5 rounded-xs bg-emerald-600 border border-emerald-700" />
            <div className="w-2.5 h-2.5 rounded-xs bg-emerald-800 border border-emerald-900" />
            <span>{t('stats.more')}</span>
          </div>
        </div>

        <div className="overflow-x-auto pb-2">
          <div className="inline-grid grid-rows-7 grid-flow-col gap-1 min-w-[700px]">
            {heatmap.map((day) => (
              <div
                key={day.date}
                className={`w-3 h-3 rounded-xs border transition-transform hover:scale-125 cursor-pointer ${getHeatmapColor(
                  day.level
                )}`}
                title={`${day.date}: ${day.reviewCount} reviews, ${day.lessonsCompleted} lessons`}
              />
            ))}
          </div>
        </div>

        <p className="text-[11px] text-stone-500">{t('stats.heatmapHint')}</p>
      </div>
    </div>
  );
};
