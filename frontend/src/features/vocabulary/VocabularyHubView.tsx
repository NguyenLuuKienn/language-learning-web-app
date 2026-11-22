import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { App } from 'antd';
import { ArrowRightOutlined, BookOutlined, ReloadOutlined, SoundOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import { getErrorMessage, srsApi, vocabularySessionsApi, type VocabularyLessonChoice } from '../../services/apiClient';
import type { StudyTarget } from '../../types';
import { VocabularyLearningView } from '../reviews/VocabularyLearningView';

interface Props {
  target: StudyTarget | null;
  onReview: () => void;
}

interface UnitGroup {
  id: string;
  title: string;
  courseTitle: string;
  sortOrder: number;
  lessons: VocabularyLessonChoice[];
}

export const VocabularyHubView: React.FC<Props> = ({ target, onReview }) => {
  const { t, language } = useTranslation();
  const { message } = App.useApp();
  const [choices, setChoices] = useState<VocabularyLessonChoice[]>([]);
  const [overview, setOverview] = useState<{ dueCount: number; learningCount: number } | null>(null);
  const [lessonId, setLessonId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [error, setError] = useState(false);

  const refresh = useCallback(async () => {
    if (!target) return;
    setLoading(true);
    setError(false);
    try {
      const [lessons, review] = await Promise.all([vocabularySessionsApi.lessons(), srsApi.getOverview()]);
      setChoices(lessons);
      setOverview(review);
    } catch (cause) {
      setError(true);
      message.error(getErrorMessage(cause, t('common.error')));
    } finally {
      setLoading(false);
    }
  }, [target, message, t]);

  useEffect(() => { void refresh(); }, [refresh]);

  const units = useMemo(() => {
    const groups = new Map<string, UnitGroup>();
    for (const lesson of choices) {
      let unit = groups.get(lesson.unitId);
      if (!unit) {
        unit = { id: lesson.unitId, title: lesson.unitTitle, courseTitle: lesson.courseTitle,
          sortOrder: lesson.unitSortOrder, lessons: [] };
        groups.set(lesson.unitId, unit);
      }
      unit.lessons.push(lesson);
    }
    return [...groups.values()].sort((a, b) => a.courseTitle.localeCompare(b.courseTitle) || a.sortOrder - b.sortOrder)
      .filter((unit) => !search.trim() || `${unit.courseTitle} ${unit.title} ${unit.lessons.map((lesson) => lesson.lessonTitle).join(' ')}`
        .toLowerCase().includes(search.trim().toLowerCase()));
  }, [choices, search]);

  const remainingGroups = choices.reduce((sum, item) => sum + Math.max(0, item.availableGroups - item.completedGroups), 0);
  const totalGroups = choices.reduce((sum, item) => sum + item.availableGroups, 0);
  const completedGroups = choices.reduce((sum, item) => sum + item.completedGroups, 0);
  const activeLesson = choices.find((item) => item.activeSessionId);

  if (!target) return <div className="rounded-xl border-notebook bg-white p-6 text-sm">{t('courses.selectTarget')}</div>;
  if (lessonId) return <VocabularyLearningView target={target} lessonId={lessonId}
    onBack={() => { setLessonId(null); void refresh(); }} />;

  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="rounded-2xl border-notebook-2 bg-white p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div><span className="text-xs font-bold uppercase tracking-wider text-amber-800">{target.languageName} · {target.level}</span>
          <h1 className="mt-2 text-2xl font-black">{t('vocabularyHub.title')}</h1>
          <p className="mt-1 text-sm text-stone-600">{t('vocabularyHub.subtitle')}</p></div>
        <button type="button" onClick={() => void refresh()} disabled={loading} className="rounded-lg border-notebook px-3 py-2 text-xs font-bold disabled:opacity-50"><ReloadOutlined /> {t('vocabularyHub.refresh')}</button>
      </div>
      <div className="mt-5 grid gap-3 sm:grid-cols-3">
        <div className="rounded-xl border border-amber-300 bg-amber-50 p-4"><strong className="block text-xl">{remainingGroups}</strong><span className="text-xs text-stone-600">{t('vocabularyHub.newGroups')}</span></div>
        <div className="rounded-xl border border-emerald-300 bg-emerald-50 p-4"><strong className="block text-xl">{overview?.dueCount ?? '—'}</strong><span className="text-xs text-stone-600">{t('vocabularyHub.due')}</span></div>
        <div className="rounded-xl border border-stone-300 bg-[#FAF6EE] p-4"><strong className="block text-xl">{overview?.learningCount ?? '—'}</strong><span className="text-xs text-stone-600">{t('vocabularyHub.scheduled')}</span></div>
      </div>
      <div className="mt-4 rounded-xl border border-stone-200 bg-[#FAF6EE] p-4">
        <div className="flex flex-wrap items-center justify-between gap-2 text-sm"><strong>{t('vocabularyHub.progress')}</strong><span>{completedGroups}/{totalGroups} {t('vocabularyHub.groups')} · {totalGroups ? Math.round(completedGroups / totalGroups * 100) : 0}%</span></div>
        <div className="mt-2 h-2 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-emerald-600" style={{ width: `${totalGroups ? completedGroups / totalGroups * 100 : 0}%` }} /></div>
        {activeLesson && <button type="button" onClick={() => setLessonId(activeLesson.lessonId)} className="mt-3 rounded-lg bg-amber-300 px-3 py-2 text-xs font-bold">{t('vocabularyHub.continue')}: {activeLesson.lessonTitle} · {activeLesson.activePosition}/{activeLesson.activeTotalSteps} <ArrowRightOutlined /></button>}
      </div>
      <button type="button" onClick={onReview} className="mt-4 rounded-xl bg-stone-900 px-4 py-3 text-sm font-bold text-white">{t('vocabularyHub.reviewDue')} <ArrowRightOutlined /></button>
    </header>

    <section className="rounded-2xl border-notebook-2 bg-white p-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div><h2 className="text-lg font-bold">{t('vocabularyHub.chooseUnit')}</h2>
          <p className="mt-1 text-xs text-stone-600">{language === 'vi' ? 'Mỗi bài là một phiên học tối đa hai vòng. Từ mới được chia cụm tối đa 3 từ; đúng hết vòng 1 có thể hoàn thành ngay.' : 'Each lesson is one session of at most two rounds. Words appear in batches of up to three; a perfect first round can finish immediately.'}</p></div>
        <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder={t('vocabularyHub.search')}
          className="w-full rounded-lg border-notebook px-3 py-2 text-sm sm:w-64" />
      </div>
      {loading && <p className="mt-5 text-sm">{t('common.loading')}</p>}
      {error && <p role="alert" className="mt-5 text-sm text-rose-700">{t('common.error')}</p>}
      {!loading && !error && units.length === 0 && <p className="mt-5 rounded-xl bg-[#FAF6EE] p-4 text-sm">{t('vocabularyHub.empty')}</p>}
      <div className="mt-5 space-y-3">{units.map((unit) => {
        const total = unit.lessons.reduce((sum, lesson) => sum + lesson.availableGroups, 0);
        const completed = unit.lessons.reduce((sum, lesson) => sum + lesson.completedGroups, 0);
        return <details key={unit.id} className="rounded-xl border border-stone-300 bg-[#FAF6EE] p-4" open={units.length === 1}>
          <summary className="cursor-pointer list-none">
            <div className="flex items-start justify-between gap-3"><div className="min-w-0"><span className="text-[11px] text-stone-500">{unit.courseTitle}</span>
              <h3 className="text-sm font-bold">{unit.title}</h3></div><span className="shrink-0 rounded-lg bg-white px-2 py-1 text-xs font-bold">{completed}/{total}</span></div>
            <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-amber-400" style={{ width: `${total ? completed / total * 100 : 0}%` }} /></div>
          </summary>
          <div className="mt-4 space-y-2">{unit.lessons.map((lesson) => <div key={lesson.lessonId} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-stone-200 bg-white p-3">
            <div className="min-w-0"><strong className="block text-sm"><BookOutlined /> {lesson.lessonTitle}</strong>
              <span className="text-xs text-stone-600">{t('vocabularyHub.groupProgress', { completed: lesson.completedGroups, total: lesson.availableGroups })}</span>
              {lesson.completedGroups < lesson.availableGroups && !!lesson.nextGroupWords?.length && <span className="mt-1 block text-xs text-stone-600">{language === 'vi' ? 'Từ sắp học' : 'Upcoming words'}: {lesson.nextGroupWords.join(' · ')}</span>}
              {lesson.activeSessionId && <span className="ml-2 text-xs font-bold text-emerald-800">{t('vocabularyHub.inProgress')} {lesson.activePosition}/{lesson.activeTotalSteps}</span>}
              <div className="mt-2 h-1.5 w-48 max-w-full overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-emerald-600" style={{ width: `${lesson.availableGroups ? (lesson.completedGroups + (lesson.activeTotalSteps ? lesson.activePosition / lesson.activeTotalSteps : 0)) / lesson.availableGroups * 100 : 0}%` }} /></div></div>
            <button type="button" disabled={lesson.completedGroups >= lesson.availableGroups} onClick={() => setLessonId(lesson.lessonId)}
              className="rounded-lg bg-amber-300 px-3 py-2 text-xs font-bold disabled:bg-stone-200 disabled:text-stone-500">
              <SoundOutlined /> {lesson.completedGroups >= lesson.availableGroups ? t('vocabularyHub.learned') : lesson.activeSessionId ? t('vocabularyHub.continue') : t('vocabularyHub.start')}
            </button>
          </div>)}</div>
        </details>;
      })}</div>
    </section>
  </div>;
};
