import React, { useState, useEffect } from 'react';
import { useTranslation } from '../../i18n';
import { coursesApi, decksApi, progressApi } from '../../services/apiClient';
import type { ResumeLearning } from '../../services/apiClient';
import { deckMatchesTarget } from '../decks/deckScope';
import type { Course, StudyTarget, UserProfile } from '../../types';
import {
  BookOutlined,
  HistoryOutlined,
  EditOutlined,
  SoundOutlined,
  QuestionCircleOutlined,
  FolderOpenOutlined,
  FireFilled,
  CheckCircleOutlined,
  RightOutlined,
  CompassOutlined,
  ArrowRightOutlined,
  GlobalOutlined
} from '@ant-design/icons';

interface HomeViewProps {
  target: StudyTarget | null;
  user: UserProfile | null;
  onOpenTargetModal: () => void;
  onNavigate: (view: 'roadmap' | 'courses' | 'decks' | 'srsReview' | 'quiz' | 'sharing' | 'writing' | 'speaking') => void;
  onResume: (courseId: string, lessonId: string, stepId?: string | null) => void;
}

export const HomeView: React.FC<HomeViewProps> = ({
  target,
  user,
  onOpenTargetModal,
  onNavigate,
  onResume
}) => {
  const { t, language } = useTranslation();
  const [recommendedCourses, setRecommendedCourses] = useState<Course[]>([]);
  const [dueCardsCount, setDueCardsCount] = useState<number>(0);
  const [isLoading, setIsLoading] = useState(false);
  const [coursesError, setCoursesError] = useState(false);
  const [coursesReloadKey, setCoursesReloadKey] = useState(0);
  const [resume, setResume] = useState<ResumeLearning | null>(null);
  const [startError, setStartError] = useState('');

  const startCourse = async (courseId: string) => {
    setStartError('');
    try {
      const [detail, roadmapResult] = await Promise.all([coursesApi.getCourseDetail(courseId), progressApi.getRoadmap().catch(() => null)]);
      const lessons = [...(detail.units || [])].sort((a, b) => a.sortOrder - b.sortOrder)
        .flatMap(unit => [...(unit.lessons || [])].sort((a, b) => a.sortOrder - b.sortOrder));
      const courseProgress = roadmapResult?.courses.find(course => course.id === courseId);
      const nextId = courseProgress?.units.flatMap(unit => unit.lessons).find(lesson => lesson.status !== 'Completed' && lesson.status !== 'Locked')?.id;
      const selected = lessons.find(lesson => lesson.id === nextId) || lessons[0];
      if (!selected) throw new Error(language === 'vi' ? 'Khóa học này chưa có bài học.' : 'This course has no lessons yet.');
      onResume(courseId, selected.id);
    } catch (cause) { setStartError(cause instanceof Error ? cause.message : (language === 'vi' ? 'Không mở được khóa học.' : 'Could not open this course.')); }
  };

  useEffect(() => {
    let isMounted = true;
    setIsLoading(true);
    setCoursesError(false);

    const domains = target ? (target.domains?.length ? target.domains : [target.domain]) : [];
    (target ? coursesApi.getCourses(target.languageId, target.level) : Promise.resolve([]))
      .then((courses) => {
        if (isMounted) {
          setRecommendedCourses(courses.filter((course) => domains.some((domain) => domain.toLowerCase() === (course.domain ?? 'general').toLowerCase())).slice(0, 3));
        }
      })
      .catch(() => {
        if (isMounted) { setRecommendedCourses([]); setCoursesError(true); }
      })
      .finally(() => {
        if (isMounted) setIsLoading(false);
      });

    (target ? decksApi.getDecks() : Promise.resolve([]))
      .then((decks) => {
        if (isMounted) {
          setDueCardsCount(decks.filter((deck) => deckMatchesTarget(deck, target)).reduce((count, deck) => count + deck.dueCards, 0));
        }
      })
      .catch(() => {
        if (isMounted) setDueCardsCount(0);
      });

    (target ? progressApi.getResume() : Promise.resolve(null))
      .then((value) => { if (isMounted) setResume(value); })
      .catch(() => { if (isMounted) setResume(null); });

    return () => {
      isMounted = false;
    };
  }, [target, coursesReloadKey]);

  if (!target) return <div className="mx-auto max-w-3xl rounded-2xl border-notebook-2 bg-white p-6">
    <h1 className="text-2xl font-bold">{language === 'vi' ? 'Bắt đầu hành trình học' : 'Start your learning journey'}</h1>
    <p className="mt-2 text-sm text-stone-600">{language === 'vi'
      ? 'Chọn ngôn ngữ, trình độ và lĩnh vực học. Bài đánh giá đầu vào sẽ giúp mở đúng lộ trình cho bạn.'
      : 'Choose your language, level and study field. The placement assessment will open the right learning path for you.'}</p>
    <button type="button" onClick={onOpenTargetModal} className="mt-5 rounded-lg bg-amber-300 px-5 py-3 text-sm font-bold">
      {language === 'vi' ? 'Chọn mục tiêu và bắt đầu' : 'Choose a goal and start'}
    </button>
  </div>;

  return (
    <div className="space-y-4">
      {resume && <button type="button" onClick={() => onResume(resume.courseId, resume.lessonId, resume.stepId)} className="w-full rounded-xl border-notebook bg-amber-100 p-4 text-left text-sm font-bold hover:bg-amber-200">{t('home.continueLearning')}: {resume.courseTitle} · {resume.lessonTitle}{resume.stepId ? ` · ${resume.stepId}` : ''} →</button>}
      {startError && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700">{startError}</p>}
      <div className="p-5 bg-white border-notebook rounded-2xl shadow-notebook flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="w-14 h-14 rounded-2xl bg-amber-100 border-2 border-amber-300 flex items-center justify-center text-2xl shadow-xs text-amber-800">
            <GlobalOutlined />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-amber-800 bg-amber-50 px-2 py-0.5 rounded border border-amber-300">
                {t('home.studyGoalBadge')}
              </span>
              <span className="text-xs font-mono font-bold text-stone-500">
                {target.level} • {target.domain.toUpperCase()}
              </span>
            </div>
            <h1 className="text-xl md:text-2xl font-extrabold text-stone-900 tracking-tight mt-0.5">
              {target.languageName}
            </h1>
            <p className="text-xs text-stone-600 mt-0.5">
              {t('home.welcomeBack', { name: user?.displayName || t('home.learner') })}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2.5 w-full md:w-auto">
          <div className="flex-1 md:flex-initial flex items-center gap-3 bg-[#FAF6EE] border-notebook px-3.5 py-2 rounded-xl">
            <div className="flex items-center gap-1.5 text-xs font-bold text-amber-600">
              <FireFilled className="text-base text-amber-500 animate-pulse" />
              <span>{user?.currentStreak ?? 0} {t('home.streakDays')}</span>
            </div>
            <span className="text-stone-300">|</span>
            <div className="flex items-center gap-1.5 text-xs font-bold text-rose-600">
              <HistoryOutlined className="text-base text-rose-500" />
              <span>{dueCardsCount} {t('home.dueCards')}</span>
            </div>
          </div>

          <button
            type="button"
            onClick={onOpenTargetModal}
            className="px-3.5 py-2 rounded-xl bg-stone-900 hover:bg-stone-800 text-white text-xs font-bold notebook-btn flex items-center gap-1.5 transition cursor-pointer shrink-0"
          >
            <CompassOutlined />
            <span>{t('home.changeTarget')}</span>
          </button>
        </div>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-2.5">
        <button
          type="button"
          onClick={() => onNavigate('roadmap')}
          className="p-3.5 bg-white border-notebook rounded-xl text-left hover:bg-stone-50 transition shadow-2xs flex flex-col justify-between h-28 cursor-pointer group"
        >
          <div className="w-8 h-8 rounded-lg bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center text-sm font-bold group-hover:scale-105 transition">
            <BookOutlined />
          </div>
          <div>
            <div className="text-xs font-bold text-stone-900 group-hover:text-blue-600 transition">
              {t('home.shortcuts.courses')}
            </div>
            <div className="text-[10px] text-stone-500 mt-0.5 truncate">
              {t('home.shortcuts.coursesDesc')}
            </div>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('writing')}
          className="p-3.5 bg-white border-notebook rounded-xl text-left hover:bg-stone-50 transition shadow-2xs flex flex-col justify-between h-28 cursor-pointer group"
        >
          <div className="w-8 h-8 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-600 flex items-center justify-center text-sm font-bold group-hover:scale-105 transition">
            <EditOutlined />
          </div>
          <div>
            <div className="text-xs font-bold text-stone-900 group-hover:text-emerald-600 transition">
              {t('home.shortcuts.writing')}
            </div>
            <div className="text-[10px] text-stone-500 mt-0.5 truncate">
              {t('home.shortcuts.writingDesc')}
            </div>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('speaking')}
          className="p-3.5 bg-white border-notebook rounded-xl text-left hover:bg-stone-50 transition shadow-2xs flex flex-col justify-between h-28 cursor-pointer group"
        >
          <div className="w-8 h-8 rounded-lg bg-indigo-50 border border-indigo-200 text-indigo-600 flex items-center justify-center text-sm font-bold group-hover:scale-105 transition">
            <SoundOutlined />
          </div>
          <div>
            <div className="text-xs font-bold text-stone-900 group-hover:text-indigo-600 transition">
              {t('home.shortcuts.speaking')}
            </div>
            <div className="text-[10px] text-stone-500 mt-0.5 truncate">
              {t('home.shortcuts.speakingDesc')}
            </div>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('srsReview')}
          className="p-3.5 bg-white border-notebook rounded-xl text-left hover:bg-stone-50 transition shadow-2xs flex flex-col justify-between h-28 cursor-pointer group"
        >
          <div className="w-8 h-8 rounded-lg bg-rose-50 border border-rose-200 text-rose-600 flex items-center justify-center text-sm font-bold group-hover:scale-105 transition">
            <HistoryOutlined />
          </div>
          <div>
            <div className="text-xs font-bold text-stone-900 group-hover:text-rose-600 transition">
              {t('home.shortcuts.srsReview')}
            </div>
            <div className="text-[10px] text-stone-500 mt-0.5 truncate">
              {t('home.shortcuts.srsReviewDesc', { count: dueCardsCount })}
            </div>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('quiz')}
          className="p-3.5 bg-white border-notebook rounded-xl text-left hover:bg-stone-50 transition shadow-2xs flex flex-col justify-between h-28 cursor-pointer group"
        >
          <div className="w-8 h-8 rounded-lg bg-amber-50 border border-amber-200 text-amber-600 flex items-center justify-center text-sm font-bold group-hover:scale-105 transition">
            <QuestionCircleOutlined />
          </div>
          <div>
            <div className="text-xs font-bold text-stone-900 group-hover:text-amber-600 transition">
              {t('home.shortcuts.quiz')}
            </div>
            <div className="text-[10px] text-stone-500 mt-0.5 truncate">
              {t('home.shortcuts.quizDesc')}
            </div>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('decks')}
          className="p-3.5 bg-white border-notebook rounded-xl text-left hover:bg-stone-50 transition shadow-2xs flex flex-col justify-between h-28 cursor-pointer group"
        >
          <div className="w-8 h-8 rounded-lg bg-purple-50 border border-purple-200 text-purple-600 flex items-center justify-center text-sm font-bold group-hover:scale-105 transition">
            <FolderOpenOutlined />
          </div>
          <div>
            <div className="text-xs font-bold text-stone-900 group-hover:text-purple-600 transition">
              {t('home.shortcuts.myDecks')}
            </div>
            <div className="text-[10px] text-stone-500 mt-0.5 truncate">
              {t('home.shortcuts.myDecksDesc')}
            </div>
          </div>
        </button>
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CheckCircleOutlined className="text-emerald-600" />
            <h2 className="text-sm font-bold uppercase tracking-wider text-stone-800">
              {t('home.recommendedCoursesTitle')}
            </h2>
          </div>
          <button
            type="button"
            onClick={() => onNavigate('roadmap')}
            className="text-xs font-bold text-stone-700 hover:text-stone-900 flex items-center gap-1 cursor-pointer"
          >
            <span>{t('home.viewAllCourses')}</span>
            <RightOutlined className="text-[10px]" />
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          {recommendedCourses.map((course) => (
            <div
              key={course.id}
              className="p-4 bg-white border-notebook rounded-xl shadow-2xs hover:shadow-xs transition flex flex-col justify-between space-y-3"
            >
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-[10px] font-mono font-bold uppercase text-indigo-700 bg-indigo-50 px-2 py-0.5 rounded border border-indigo-200">
                    {course.level} • {(course.domain || 'general').toUpperCase()}
                  </span>
                  <span className="text-[11px] font-mono text-stone-500">
                    {course.unitsCount} {t('courses.units')} • {course.totalLessons} {t('courses.lessons')}
                  </span>
                </div>
                <h3 className="text-sm font-bold text-stone-900 leading-snug">
                  {course.title}
                </h3>
                <p className="text-xs text-stone-600 line-clamp-2">
                  {course.description}
                </p>
              </div>

              <button
                type="button"
                onClick={() => void startCourse(course.id)}
                className="w-full py-2 rounded-lg bg-[#FAF6EE] hover:bg-stone-200/70 border border-stone-300 text-stone-900 text-xs font-bold flex items-center justify-center gap-1.5 transition cursor-pointer"
              >
                <span>{t('home.startLearning')}</span>
                <ArrowRightOutlined className="text-[10px]" />
              </button>
            </div>
          ))}

          {coursesError && !isLoading && <div role="alert" className="col-span-3 rounded-xl border border-rose-300 bg-white p-5 text-sm">
            <p>{language === 'vi' ? 'Không tải được danh sách khóa học.' : 'Could not load courses.'}</p>
            <button type="button" onClick={() => setCoursesReloadKey((value) => value + 1)} className="mt-2 rounded-lg bg-amber-300 px-3 py-2 font-bold">{language === 'vi' ? 'Thử lại' : 'Retry'}</button>
          </div>}
          {recommendedCourses.length === 0 && !isLoading && !coursesError && (
            <div className="col-span-3 p-8 bg-white border-notebook rounded-xl text-center space-y-2">
              <p className="text-xs text-stone-500">{t('home.noCoursesForTarget')}</p>
              <button
                type="button"
                onClick={() => onNavigate('roadmap')}
                className="px-4 py-2 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn cursor-pointer"
              >
                {t('home.exploreAllCurriculum')}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
