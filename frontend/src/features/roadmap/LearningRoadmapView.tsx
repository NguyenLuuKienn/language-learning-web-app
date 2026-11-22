import React, { useEffect, useState } from 'react';
import { ArrowRightOutlined, CompassOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { lessonJourneyApi, progressApi, type LearningRoadmap, type LessonJourney } from '../../services/apiClient';

interface Props {
  target: StudyTarget | null;
  onNavigate: (view: string) => void;
  onOpenTargetModal: () => void;
  onResume: (courseId: string, lessonId: string) => void;
}

export const LearningRoadmapView: React.FC<Props> = ({ target, onNavigate, onOpenTargetModal, onResume }) => {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const [roadmap, setRoadmap] = useState<LearningRoadmap | null>(null);
  const [nextJourney, setNextJourney] = useState<LessonJourney | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const load = async () => {
    setLoading(true); setError(false);
    try {
      const result = await progressApi.getRoadmap();
      setRoadmap(result);
      if (result.nextLessonId) {
        try { setNextJourney(await lessonJourneyApi.get(result.nextLessonId)); }
        catch { setNextJourney(null); }
      } else setNextJourney(null);
    } catch { setError(true); }
    finally { setLoading(false); }
  };
  useEffect(() => { if (target) void load(); else { setRoadmap(null); setNextJourney(null); } }, [target]);
  const percent = roadmap?.totalLessons ? Math.round(roadmap.completedLessons / roadmap.totalLessons * 100) : 0;
  return <div className="mx-auto max-w-5xl space-y-5">
    <header className="rounded-2xl border-notebook-2 bg-white p-6">
      <div className="flex flex-wrap items-start justify-between gap-3"><div><p className="text-xs font-bold uppercase text-amber-800">{target ? `${target.languageName} · ${target.level} · ${target.domain}` : ''}</p><h1 className="mt-2 text-2xl font-black">{vi ? 'Hành trình học' : 'Learning journey'}</h1><p className="mt-1 text-sm text-stone-600">{vi ? 'Học theo khóa → chương → bài → từng bước. Các công cụ luyện tập vẫn có thể mở riêng.' : 'Follow course → unit → lesson → steps. Practice tools remain available separately.'}</p></div><button type="button" onClick={onOpenTargetModal} className="rounded-lg border-notebook bg-amber-300 px-4 py-2 text-xs font-bold"><CompassOutlined /> {vi ? 'Đổi mục tiêu' : 'Change goal'}</button></div>
      {roadmap && <div className="mt-5"><div className="flex justify-between text-sm font-bold"><span>{vi ? 'Tiến độ bài học' : 'Lesson progress'}</span><span>{roadmap.completedLessons}/{roadmap.totalLessons} · {percent}%</span></div><div className="mt-2 h-2 overflow-hidden rounded-full bg-stone-200"><div className="h-full bg-emerald-600" style={{ width: `${percent}%` }} /></div></div>}
      {roadmap?.nextCourseId && roadmap.nextLessonId && <button type="button" onClick={() => onResume(roadmap.nextCourseId!, roadmap.nextLessonId!)} className="mt-5 rounded-lg bg-amber-300 px-5 py-3 text-sm font-bold">{nextJourney?.status === 'InProgress' ? (vi ? 'Tiếp tục' : 'Continue') : (vi ? 'Bắt đầu' : 'Start')}: {nextJourney?.lessonTitle || ''}{nextJourney?.currentStepId ? ` · ${nextJourney.steps.find(step => step.id === nextJourney.currentStepId)?.title || nextJourney.currentStepId}` : ''} <ArrowRightOutlined /></button>}
    </header>
    {loading && <p className="rounded-xl border-notebook bg-white p-5">{vi ? 'Đang tải lộ trình...' : 'Loading roadmap...'}</p>}
    {error && <div role="alert" className="rounded-xl border border-rose-300 bg-white p-5"><p>{vi ? 'Không tải được lộ trình.' : 'Could not load the roadmap.'}</p><button type="button" onClick={() => void load()} className="mt-3 rounded-lg bg-amber-300 px-3 py-2 text-xs">{vi ? 'Thử lại' : 'Retry'}</button></div>}
    {!loading && !error && roadmap?.courses.length === 0 && <p className="rounded-xl border-notebook bg-white p-5 text-sm">{vi ? 'Mục tiêu hiện tại chưa có khóa học.' : 'No courses match your current goal.'}</p>}
    {roadmap?.courses.map(course => <section key={course.id} className="rounded-2xl border-notebook-2 bg-white p-5">
      <div className="flex flex-wrap items-center justify-between gap-2"><div><p className="text-xs font-bold text-amber-800">{course.level} · {course.domain}</p><h2 className="mt-1 text-lg font-bold">{course.title}</h2></div><span className="rounded-lg bg-[#FAF6EE] px-3 py-2 text-xs font-bold">{course.completedLessons}/{course.totalLessons} {vi ? 'bài' : 'lessons'}</span></div>
      <div className="mt-5 space-y-3">{course.units.map((unit, unitIndex) => {
        const completed = unit.lessons.filter(lesson => lesson.status === 'Completed').length;
        return <details key={unit.id} open={unit.lessons.some(lesson => lesson.id === roadmap.nextLessonId)} className="rounded-xl border border-stone-300 bg-[#FAF6EE] p-4"><summary className="cursor-pointer font-bold">{vi ? 'Chương' : 'Unit'} {unitIndex + 1}: {unit.title} <span className="ml-2 text-xs font-normal text-stone-500">{completed}/{unit.lessons.length}</span></summary><div className="mt-4 space-y-2">{unit.lessons.map((lesson, index) => <button key={lesson.id} type="button" disabled={lesson.status === 'Locked'} onClick={() => onResume(course.id, lesson.id)} className="flex w-full items-center justify-between gap-3 rounded-lg border border-stone-200 bg-white p-3 text-left text-sm hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-50"><span><strong>{vi ? 'Bài' : 'Lesson'} {index + 1}: {lesson.title}</strong><small className="mt-1 block text-stone-500">{lesson.status === 'Completed' ? (vi ? 'Đã hoàn thành' : 'Completed') : lesson.status === 'InProgress' ? (vi ? 'Đang học' : 'In progress') : lesson.status === 'Locked' ? (vi ? 'Hoàn thành bài trước để mở' : 'Complete the previous lesson') : (vi ? 'Sẵn sàng học' : 'Ready')}</small></span><span aria-hidden="true">{lesson.status === 'Completed' ? '✓' : lesson.status === 'Locked' ? '🔒' : '→'}</span></button>)}</div></details>;
      })}</div>
    </section>)}
    <section className="rounded-2xl border-notebook bg-white p-5"><h2 className="font-bold">{vi ? 'Công cụ luyện tập riêng' : 'Independent practice tools'}</h2><p className="mt-1 text-xs text-stone-500">{vi ? 'Dùng thêm khi cần. Hành trình chính nằm trong từng bài học.' : 'Use these when needed. Your main journey is inside each lesson.'}</p><div className="mt-4 flex flex-wrap gap-2">{[
      ['courses', vi ? 'Từ vựng' : 'Vocabulary'], ['listening', vi ? 'Luyện nghe' : 'Listening'],
      ['speaking', vi ? 'Luyện nói' : 'Speaking'], ['reading', vi ? 'Luyện đọc' : 'Reading'],
      ['writing', vi ? 'Luyện viết' : 'Writing'], ['grammar', vi ? 'Ngữ pháp' : 'Grammar'],
      ['srsReview', vi ? 'Ôn tập ghi nhớ' : 'Memory review'], ['certificatePractice', 'IELTS / TOEIC'],
      ['interviewPractice', vi ? 'Phỏng vấn' : 'Interview']
    ].map(([view, title]) => <button key={view} type="button" onClick={() => onNavigate(view)} className="rounded-lg border-notebook px-3 py-2 text-xs hover:bg-amber-50">{title} <ArrowRightOutlined /></button>)}</div></section>
  </div>;
};
