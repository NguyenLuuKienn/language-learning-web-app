import React, { useEffect, useState } from 'react';
import { useTranslation } from '../../i18n';
import { coursesApi } from '../../services/apiClient';
import type { CourseDetail, StudyTarget } from '../../types';
import { sortCourses, sortLessons, sortUnits } from '../../utils/sorting';
import { Pagination } from '../../components/ui/Pagination';

interface QuizHubViewProps {
  onStartQuiz: (lessonId: string, lessonTitle: string) => void;
  onNavigateToCourses: () => void;
  onStartPlacement: () => void;
  target: StudyTarget | null;
}

export const QuizHubView: React.FC<QuizHubViewProps> = ({ onStartQuiz, onNavigateToCourses, onStartPlacement, target }) => {
  const { t } = useTranslation();
  const [courses, setCourses] = useState<CourseDetail[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const lessons = courses.flatMap((course) => sortUnits(course.units).flatMap((unit) =>
    sortLessons(unit.lessons).map((lesson) => ({ course, unit, lesson }))));
  const pageCount = Math.max(1, Math.ceil(lessons.length / 8));
  const visibleLessons = lessons.slice((Math.min(page, pageCount) - 1) * 8, Math.min(page, pageCount) * 8);

  useEffect(() => {
    let active = true;
    if (!target) {
      setCourses([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    setPage(1);
    setError('');
    coursesApi.getCourses(target.languageId, target.level).then(async (items) => {
      const domains = target?.domains?.length ? target.domains : target?.domain ? [target.domain] : [];
      const eligible = items.filter((item) => item.status === 'Published' &&
        item.languageId === target.languageId && item.level.toLowerCase() === target.level.toLowerCase() &&
        domains.some((domain) => domain.toLowerCase() === (item.domain || 'general').toLowerCase()));
      const details = await Promise.all(eligible.map((item) => coursesApi.getCourseDetail(item.id)));
      if (active) setCourses(sortCourses(details));
    }).catch((cause) => {
      if (active) setError(cause instanceof Error ? cause.message : t('common.error'));
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [t, target]);

  return <div className="max-w-4xl mx-auto space-y-6">
    <section className="bg-white border-notebook-2 rounded-2xl p-6 space-y-3">
      <h1 className="text-xl font-bold">{t('nav.quiz')}</h1>
      <p className="text-sm text-stone-600">{t('quiz.chooseRealLesson')}</p>
      <button onClick={onStartPlacement} className="px-4 py-2 rounded-lg bg-amber-300 font-bold text-sm">{t('quiz.placementTest')}</button>
    </section>
    {loading && <p className="text-sm text-stone-600">{t('common.loading')}</p>}
    {error && <p role="alert" className="text-sm text-rose-700">{error}</p>}
    {!loading && !error && courses.length === 0 && <p className="text-sm">{t('quiz.noCourses')}</p>}
    {courses.filter((course) => visibleLessons.some((item) => item.course.id === course.id)).map((course) => <section key={course.id} className="bg-white border-notebook-2 rounded-2xl p-5 space-y-3">
      <h2 className="font-bold">{course.title} · {course.level}</h2>
      {sortUnits(course.units).flatMap((unit) => sortLessons(unit.lessons)).filter((lesson) => visibleLessons.some((item) => item.lesson.id === lesson.id)).map((lesson) => <div key={lesson.id} className="flex items-center justify-between gap-3 border-t border-stone-200 pt-3">
        <span className="text-sm">{lesson.title}</span>
        <button onClick={() => onStartQuiz(lesson.id, lesson.title)} className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white">{t('quiz.startLessonQuiz')}</button>
      </div>)}
    </section>)}
    <Pagination page={Math.min(page, pageCount)} pageCount={pageCount} onChange={setPage} />
    <button onClick={onNavigateToCourses} className="text-sm font-bold underline">{t('quiz.browseCourses')}</button>
  </div>;
};
