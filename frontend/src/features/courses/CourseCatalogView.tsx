import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useTranslation } from '../../i18n';
import { coursesApi, progressApi, vocabularySessionsApi, type VocabularyLessonChoice } from '../../services/apiClient';
import { curriculumStore } from '../../services/curriculumStore';
import { speechService } from '../../services/speechService';
import type { Course, CourseDetail, Language, LessonDetail, StudyTarget } from '../../types';
import { sortUnits, sortLessons, sortCourses } from '../../utils/sorting';
import { VocabularyLearningView } from '../reviews/VocabularyLearningView';
import {
  BookOutlined,
  SoundOutlined,
  PlayCircleOutlined,
  CheckCircleOutlined,
  ArrowLeftOutlined,
  LoadingOutlined,
  DownOutlined,
  RightOutlined,
  SearchOutlined,
  CloseCircleFilled,
  AppstoreOutlined,
  UnorderedListOutlined,
  LeftOutlined,
  FolderOutlined,
  FolderOpenOutlined
} from '@ant-design/icons';

interface CourseCatalogViewProps {
  target?: StudyTarget | null;
  isAdmin?: boolean;
  onStartQuiz: (lessonId: string, lessonTitle: string) => void;
}

export const CourseCatalogView: React.FC<CourseCatalogViewProps> = ({ target, isAdmin = false, onStartQuiz }) => {
  const { t, language } = useTranslation();
  const [courses, setCourses] = useState<Course[]>([]);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [selectedCourse, setSelectedCourse] = useState<CourseDetail | null>(null);
  const [selectedLesson, setSelectedLesson] = useState<LessonDetail | null>(null);
  const [lessonCompleted, setLessonCompleted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [speakingTerm, setSpeakingTerm] = useState<string | null>(null);
  const [vocabularyChoices, setVocabularyChoices] = useState<VocabularyLessonChoice[]>([]);
  const [activeVocabularyLessonId, setActiveVocabularyLessonId] = useState<string | null>(null);

  // Bộ lọc ngôn ngữ khóa học, tự động thích ứng theo target
  const [languageFilter, setLanguageFilter] = useState<string>(
    !isAdmin && target?.languageCode ? target.languageCode.toLowerCase() : 'all'
  );

  useEffect(() => {
    if (!isAdmin && target?.languageCode) {
      setLanguageFilter(target.languageCode.toLowerCase());
    } else if (isAdmin) {
      setLanguageFilter('all');
    }
  }, [target?.languageCode, isAdmin]);

  // States for handling long unit lists (e.g. 45 units)
  const [expandedUnitIds, setExpandedUnitIds] = useState<Set<string>>(new Set());
  const [searchQuery, setSearchQuery] = useState('');
  const [viewMode, setViewMode] = useState<'list' | 'matrix'>('list');
  const [activeUnitId, setActiveUnitId] = useState<string | null>(null);

  const unitRefs = useRef<Map<string, HTMLDivElement>>(new Map());
  const resumeOpened = useRef(false);

  useEffect(() => {
    let active = true;
    setSelectedCourse(null);
    setSelectedLesson(null);
    setActiveVocabularyLessonId(null);
    setVocabularyChoices([]);
    setCourses([]);
    if (!target && !isAdmin) {
      setLanguages([]);
      setIsLoading(false);
      return () => { active = false; };
    }
    setIsLoading(true);
    Promise.all([coursesApi.getCourses(isAdmin ? undefined : target!.languageId, isAdmin ? undefined : target!.level), coursesApi.getLanguages()])
      .then(([availableCourses, availableLanguages]) => {
        if (!active) return;
        const allowedDomains = target?.domains?.length ? target.domains : target?.domain ? [target.domain] : [];
        const eligible = availableCourses.filter((course) =>
          (course.hasMaterial || course.unitsCount > 0) &&
          (isAdmin || allowedDomains.some((domain) => domain.toLowerCase() === (course.domain ?? 'general').toLowerCase()))
        );
        setCourses(sortCourses(eligible));
        setLanguages(availableLanguages);
      })
      .catch(() => {
        if (active) {
          setCourses([]);
          setLanguages([]);
        }
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });
    return () => { active = false; };
  }, [target, isAdmin]);

  const handleSelectCourse = async (courseId: string, lessonId?: string) => {
    if (!courses.some((course) => course.id === courseId)) return;
    setIsLoading(true);
    try {
      const detail = await coursesApi.getCourseDetail(courseId);
      const sortedUnits = sortUnits(detail.units).map((u) => ({
        ...u,
        lessons: sortLessons(u.lessons)
      }));
      const sortedDetail: CourseDetail = {
        ...detail,
        units: sortedUnits
      };
      setSelectedCourse(sortedDetail);
      setSelectedLesson(null);
      setActiveVocabularyLessonId(null);
      setSearchQuery('');
      if (!isAdmin) void vocabularySessionsApi.lessons().then(setVocabularyChoices).catch(() => setVocabularyChoices([]));

      if (sortedUnits.length > 0) {
        const firstUnit = sortedUnits[0];
        const activeUnit = sortedUnits.find((unit) => unit.lessons.some((item) => item.id === lessonId)) ?? firstUnit;
        setExpandedUnitIds(new Set([activeUnit.id]));
        setActiveUnitId(activeUnit.id);
        if (lessonId && sortedUnits.some((unit) => unit.lessons.some((item) => item.id === lessonId)))
          void handleSelectLesson(lessonId);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleSelectLesson = async (lessonId: string) => {
    setIsLoading(true);
    try {
      let lesson: LessonDetail | null = null;
      try {
        lesson = await coursesApi.getLessonDetail(lessonId);
      } catch {
        if (selectedCourse) {
          const c = curriculumStore.getCourseById(selectedCourse.id);
          const curLesson = c?.units.flatMap((u) => u.lessons).find((l) => l.id === lessonId);
          if (curLesson) {
            lesson = {
              id: curLesson.id,
              unitId: 'unit-1',
              courseId: selectedCourse.id,
              title: curLesson.title,
              sortOrder: curLesson.order,
              status: 'NotStarted',
              vocabularies: (curLesson.flashcards || []).map((f) => ({
                id: f.id,
                lessonId: curLesson.id,
                term: f.front,
                phonetic: f.phonetic || '',
                meaning: f.back,
                example: f.example || ''
              }))
            };
          }
        }
      }

      if (lesson) {
        setSelectedLesson(lesson);
        setLessonCompleted(false);
        if (!isAdmin) {
          void progressApi.getLesson(lessonId)
            .then((progress) => {
              setLessonCompleted(progress.status === 'Completed');
              return progress.status === 'Completed' ? undefined : progressApi.updateLesson(lessonId, 'InProgress');
            })
            .catch(() => undefined);
        }
        // Auto expand & activate parent unit
        if (selectedCourse) {
          const parentUnit = selectedCourse.units.find((u) =>
            u.lessons.some((l) => l.id === lessonId)
          );
          if (parentUnit) {
            setActiveUnitId(parentUnit.id);
            setExpandedUnitIds((prev) => new Set([...prev, parentUnit.id]));
          }
        }
      }
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (resumeOpened.current || courses.length === 0) return;
    const params = new URLSearchParams(window.location.search);
    const courseId = params.get('courseId');
    const lessonId = params.get('lessonId');
    if (courseId && courses.some((course) => course.id === courseId)) {
      resumeOpened.current = true;
      void handleSelectCourse(courseId, lessonId ?? undefined);
    }
  }, [courses]);

  // Sync active unit if selectedLesson changes
  useEffect(() => {
    if (selectedLesson && selectedCourse) {
      const parentUnit = selectedCourse.units.find((u) =>
        u.lessons.some((l) => l.id === selectedLesson.id)
      );
      if (parentUnit) {
        setActiveUnitId(parentUnit.id);
        setExpandedUnitIds((prev) => new Set([...prev, parentUnit.id]));
      }
    }
  }, [selectedLesson?.id, selectedCourse]);

  const toggleUnit = (unitId: string) => {
    setActiveUnitId(unitId);
    setSelectedLesson(null);
    setExpandedUnitIds((prev) => {
      const next = new Set(prev);
      if (next.has(unitId)) {
        next.delete(unitId);
      } else {
        next.add(unitId);
      }
      return next;
    });
  };

  const expandAllUnits = () => {
    if (selectedCourse) {
      setExpandedUnitIds(new Set(selectedCourse.units.map((u) => u.id)));
    }
  };

  const collapseAllUnits = () => {
    setExpandedUnitIds(new Set());
  };

  // Quick jump to unit
  const handleJumpToUnit = (unitId: string) => {
    if (!unitId || !selectedCourse) return;
    setActiveUnitId(unitId);
    setSelectedLesson(null);
    setExpandedUnitIds((prev) => new Set([...prev, unitId]));

    // Scroll smoothly to unit element if in list mode
    if (viewMode === 'list') {
      setTimeout(() => {
        const el = unitRefs.current.get(unitId);
        if (el) {
          el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
      }, 50);
    }
  };

  // Filtered units based on search query
  const filteredUnits = useMemo(() => {
    if (!selectedCourse) return [];
    const rawUnits = !searchQuery.trim()
      ? selectedCourse.units
      : selectedCourse.units
          .map((unit) => {
            const q = searchQuery.toLowerCase().trim();
            const titleMatch = unit.title.toLowerCase().includes(q);
            const matchingLessons = unit.lessons.filter((l) =>
              l.title.toLowerCase().includes(q)
            );
            if (titleMatch) {
              return unit;
            }
            if (matchingLessons.length > 0) {
              return {
                ...unit,
                lessons: sortLessons(matchingLessons)
              };
            }
            return null;
          })
          .filter((u): u is CourseDetail['units'][0] => u !== null);

    return sortUnits(rawUnits);
  }, [selectedCourse, searchQuery]);

  // Auto-expand filtered units on search
  useEffect(() => {
    if (searchQuery.trim() && filteredUnits.length > 0) {
      setExpandedUnitIds(new Set(filteredUnits.map((u) => u.id)));
    }
  }, [searchQuery, filteredUnits]);

  // Flattened list of all lessons for Next/Prev navigation
  const allLessons = useMemo(() => {
    if (!selectedCourse) return [];
    return sortUnits(selectedCourse.units).flatMap((u) =>
      sortLessons(u.lessons).map((l) => ({ ...l, unitId: u.id, unitTitle: u.title }))
    );
  }, [selectedCourse]);

  const currentLessonIndex = useMemo(() => {
    if (!selectedLesson || allLessons.length === 0) return -1;
    return allLessons.findIndex((l) => l.id === selectedLesson.id);
  }, [selectedLesson, allLessons]);

  const prevLesson = currentLessonIndex > 0 ? allLessons[currentLessonIndex - 1] : null;
  const nextLesson =
    currentLessonIndex >= 0 && currentLessonIndex < allLessons.length - 1
      ? allLessons[currentLessonIndex + 1]
      : null;

  // Total lessons count
  const totalLessonsCount = useMemo(() => {
    if (!selectedCourse) return 0;
    return selectedCourse.units.reduce((acc, u) => acc + u.lessons.length, 0);
  }, [selectedCourse]);

  // Active unit for matrix view
  const currentMatrixUnit = useMemo(() => {
    if (!selectedCourse) return null;
    return (
      selectedCourse.units.find((u) => u.id === activeUnitId) ||
      selectedCourse.units[0] ||
      null
    );
  }, [selectedCourse, activeUnitId]);

  const speak = (term: string) => {
    const lang = selectedCourse?.languageCode ?? (languageFilter !== 'all' ? languageFilter : target?.languageCode || 'en');
    speechService.speak(term, lang, {
      rate: 0.9,
      onStart: () => setSpeakingTerm(term),
      onEnd: () => setSpeakingTerm(null),
      onError: () => setSpeakingTerm(null)
    });
  };

  // Lọc khóa học theo tab ngôn ngữ được chọn
  const filteredCourses = useMemo(() => {
    if (!languageFilter || languageFilter === 'all') return courses;
    return courses.filter((course) => course.languageCode?.toLowerCase() === languageFilter);
  }, [courses, languageFilter]);

  if (isLoading && courses.length === 0) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <LoadingOutlined className="text-3xl text-stone-500 animate-spin" />
      </div>
    );
  }

  if (activeVocabularyLessonId && target && !isAdmin) {
    return <VocabularyLearningView target={target} lessonId={activeVocabularyLessonId}
      onBack={() => {
        setActiveVocabularyLessonId(null);
        void vocabularySessionsApi.lessons().then(setVocabularyChoices).catch(() => undefined);
        void progressApi.getLesson(activeVocabularyLessonId).then((progress) => setLessonCompleted(progress.status === 'Completed')).catch(() => undefined);
      }} />;
  }

  if (selectedCourse) {
    return (
      <div className="space-y-4">
        {/* Top Header Bar */}
        <div className="flex flex-wrap items-center justify-between gap-3 pb-3 border-b-2 border-stone-300">
          <button
            onClick={() => {
              setSelectedCourse(null);
              setSelectedLesson(null);
            }}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold text-stone-700 hover:bg-stone-200 transition notebook-btn bg-white cursor-pointer"
          >
            <ArrowLeftOutlined />
            <span>{t('common.back')}</span>
          </button>

          <div className="flex items-center gap-2">
            <span className="text-xs uppercase font-mono font-bold px-2 py-0.5 rounded bg-amber-100 text-amber-800 border border-amber-300">
              {selectedCourse.level}
            </span>
            <span className="text-xs font-bold text-stone-600 bg-white border-notebook px-2 py-0.5 rounded">
              {selectedCourse.units.length} {t('courses.units')} • {totalLessonsCount} {t('courses.lessons')}
            </span>
          </div>
        </div>

        {/* 2-Column Responsive Layout with Sticky Sidebar */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
          {/* Left Column: Units Navigation (Sticky & Independently Scrollable) */}
          <div className="lg:col-span-4 lg:sticky lg:top-4 self-start max-h-[calc(100vh-5rem)] flex flex-col">
            <div className="bg-white border-notebook-2 rounded-2xl p-3.5 shadow-xs flex flex-col h-full max-h-[calc(100vh-5rem)] overflow-hidden">
              {/* Course Title & Short Info */}
              <div className="shrink-0 pb-2.5 border-b border-stone-200">
                <h2 className="text-sm font-bold text-stone-900 leading-snug line-clamp-1" title={selectedCourse.title}>
                  {selectedCourse.title}
                </h2>
                <p className="text-[11px] text-stone-500 line-clamp-1 mt-0.5">
                  {selectedCourse.description}
                </p>

                <div className="mt-2.5 flex min-w-0 items-center gap-2">
                  <div className="relative min-w-0 flex-1">
                    <input
                      type="text"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      placeholder={t('courses.searchPlaceholder')}
                      className="w-full pl-8 pr-7 py-1.5 bg-stone-50 border-notebook rounded-lg text-xs text-stone-800 placeholder-stone-400 focus:outline-none focus:bg-white focus:ring-1 focus:ring-amber-400 transition"
                    />
                    <SearchOutlined className="absolute left-2.5 top-1/2 -translate-y-1/2 text-stone-400 text-xs pointer-events-none" />
                    {searchQuery && (
                      <button
                        onClick={() => setSearchQuery('')}
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-stone-400 hover:text-stone-600 cursor-pointer"
                        title={t('courses.clearSearch')}
                      >
                        <CloseCircleFilled className="text-xs" />
                      </button>
                    )}
                  </div>

                  <div className="flex shrink-0 items-center overflow-hidden rounded-lg border-notebook bg-stone-100" role="group" aria-label={t('courses.viewMode')}>
                    <button
                      type="button"
                      onClick={() => setViewMode('list')}
                      aria-label={t('courses.listView')}
                      aria-pressed={viewMode === 'list'}
                      className={`px-2 py-1 text-xs transition cursor-pointer ${
                        viewMode === 'list'
                          ? 'bg-amber-400 text-stone-900 font-bold'
                          : 'text-stone-600 hover:bg-stone-200'
                      }`}
                      title={t('courses.listView')}
                    >
                      <UnorderedListOutlined />
                    </button>
                    <button
                      type="button"
                      onClick={() => setViewMode('matrix')}
                      aria-label={t('courses.matrixView')}
                      aria-pressed={viewMode === 'matrix'}
                      className={`px-2 py-1 text-xs transition cursor-pointer ${
                        viewMode === 'matrix'
                          ? 'bg-amber-400 text-stone-900 font-bold'
                          : 'text-stone-600 hover:bg-stone-200'
                      }`}
                      title={`${t('courses.matrixView')} (${selectedCourse.units.length} ${t('courses.units')})`}
                    >
                      <AppstoreOutlined />
                    </button>
                  </div>
                </div>

                {/* Controls for List Mode */}
                {viewMode === 'list' && (
                  <div className="mt-2 flex items-center justify-between text-[11px] text-stone-500 font-semibold pt-1">
                    <span>
                      {filteredUnits.length} / {selectedCourse.units.length} {t('courses.units')}
                    </span>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={collapseAllUnits}
                        className="hover:text-stone-900 flex items-center gap-1 transition cursor-pointer"
                        title={t('courses.collapseAll')}
                      >
                        <FolderOutlined />
                        <span>{t('courses.collapseAll')}</span>
                      </button>
                      <span className="text-stone-300">|</span>
                      <button
                        onClick={expandAllUnits}
                        className="hover:text-stone-900 flex items-center gap-1 transition cursor-pointer"
                        title={t('courses.expandAll')}
                      >
                        <FolderOpenOutlined />
                        <span>{t('courses.expandAll')}</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>

              {/* Scrollable Unit Content */}
              <div className="flex-1 overflow-y-auto notebook-scrollbar pr-1 pt-2.5 space-y-2">
                {viewMode === 'matrix' ? (
                  /* ================= MATRIX VIEW (SUPER COMPACT 45 UNITS) ================= */
                  <div className="space-y-3">
                    <div className="bg-stone-50/70 border-notebook rounded-xl p-2.5">
                      <div className="text-[10px] font-bold uppercase tracking-wider text-stone-500 mb-1.5 flex items-center justify-between">
                        <span>{t('courses.matrixView')}</span>
                        <span className="font-mono">{selectedCourse.units.length} {t('courses.units')}</span>
                      </div>
                      <div className="grid grid-cols-6 sm:grid-cols-7 gap-1.5 max-h-44 overflow-y-auto notebook-scrollbar pr-1 p-0.5">
                        {selectedCourse.units.map((unit, index) => {
                          const isActive = activeUnitId === unit.id;
                          const hasActiveLesson = unit.lessons.some(
                            (l) => l.id === selectedLesson?.id
                          );
                          const isCompleted =
                            unit.lessons.length > 0 &&
                            unit.lessons.every((l) => l.status === 'Completed');

                          return (
                            <button
                              key={unit.id}
                              onClick={() => handleJumpToUnit(unit.id)}
                              className={`h-8 rounded-lg text-xs font-bold font-mono transition flex flex-col items-center justify-center cursor-pointer border ${
                                isActive
                                  ? 'bg-amber-400 text-stone-900 border-stone-900 shadow-2xs scale-105'
                                  : hasActiveLesson
                                  ? 'bg-amber-100 text-stone-800 border-amber-400'
                                  : isCompleted
                                  ? 'bg-emerald-50 text-emerald-800 border-emerald-300'
                                  : 'bg-white text-stone-700 border-stone-300 hover:bg-stone-100'
                              }`}
                              title={unit.title || `Unit ${index + 1}`}
                            >
                              <span>{String(index + 1).padStart(2, '0')}</span>
                            </button>
                          );
                        })}
                      </div>
                    </div>

                    {/* Lessons list for currently selected Unit in Matrix view */}
                    {currentMatrixUnit && (
                      <div className="bg-amber-50/50 border-notebook rounded-xl p-2.5 space-y-1.5">
                        <div className="flex items-center justify-between pb-1.5 border-b border-amber-200">
                          <span className="text-xs font-bold text-stone-900 truncate" title={currentMatrixUnit.title}>
                            {currentMatrixUnit.title}
                          </span>
                          <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-amber-200 text-amber-900 shrink-0">
                             {currentMatrixUnit.lessons.length} {language === 'vi' ? 'bài' : 'lessons'}
                          </span>
                        </div>
                        <div className="space-y-1 pt-1">
                          {currentMatrixUnit.lessons.map((lesson) => (
                            <button
                              key={lesson.id}
                              onClick={() => handleSelectLesson(lesson.id)}
                              className={`w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                                selectedLesson?.id === lesson.id
                                  ? 'bg-amber-300 border-notebook text-stone-900 shadow-2xs font-bold'
                                  : 'bg-white hover:bg-amber-100/70 text-stone-700 border border-stone-200'
                              }`}
                            >
                              <div className="flex items-center gap-2 truncate">
                                <BookOutlined className="text-stone-400 text-xs shrink-0" />
                                <span className="truncate">{lesson.title}</span>
                              </div>
                              {lesson.status === 'Completed' && (
                                <CheckCircleOutlined className="text-emerald-600 text-xs shrink-0" />
                              )}
                            </button>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  /* ================= LIST ACCORDION VIEW ================= */
                  <div className="space-y-2">
                    {filteredUnits.length === 0 ? (
                      <div className="p-4 text-center text-xs text-stone-500 bg-stone-50 rounded-xl border border-dashed border-stone-300">
                        {t('courses.noUnitsFound')}
                      </div>
                    ) : (
                      filteredUnits.map((unit) => {
                        const isExpanded = expandedUnitIds.has(unit.id);
                        const hasActiveLesson = unit.lessons.some(
                          (l) => l.id === selectedLesson?.id
                        );
                        const isCompleted =
                          unit.lessons.length > 0 &&
                          unit.lessons.every((l) => l.status === 'Completed');

                        return (
                          <div
                            key={unit.id}
                            ref={(el) => {
                              if (el) unitRefs.current.set(unit.id, el);
                              else unitRefs.current.delete(unit.id);
                            }}
                            className={`rounded-xl border transition overflow-hidden ${
                              hasActiveLesson
                                ? 'border-notebook bg-amber-50/40 shadow-2xs'
                                : 'border-stone-200 bg-white hover:border-stone-300'
                            }`}
                          >
                            {/* Accordion Unit Header Button */}
                            <button
                              onClick={() => toggleUnit(unit.id)}
                              className={`w-full flex items-center justify-between px-3 py-2 text-left cursor-pointer transition select-none ${
                                isExpanded
                                  ? hasActiveLesson
                                    ? 'bg-amber-100/70 text-stone-900 font-bold border-b border-notebook'
                                    : 'bg-stone-50 text-stone-900 font-bold border-b border-stone-200'
                                  : hasActiveLesson
                                  ? 'text-stone-900 font-bold'
                                  : 'text-stone-700 hover:bg-stone-50'
                              }`}
                            >
                              <div className="flex items-center gap-2 truncate pr-2">
                                <span className="text-stone-400 text-xs shrink-0">
                                  {isExpanded ? <DownOutlined /> : <RightOutlined />}
                                </span>
                                <span className="text-xs truncate" title={unit.title}>
                                  {unit.title}
                                </span>
                              </div>

                              <div className="flex items-center gap-1.5 shrink-0">
                                {isCompleted && (
                                  <CheckCircleOutlined className="text-emerald-500 text-xs" />
                                )}
                                <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-stone-100 text-stone-600 font-medium">
                                  {unit.lessons.length}
                                </span>
                              </div>
                            </button>

                            {/* Accordion Unit Lessons Body */}
                            {isExpanded && (
                              <div className="p-1.5 space-y-1 bg-white">
                                {unit.lessons.map((lesson) => {
                                  const isLessonActive = selectedLesson?.id === lesson.id;
                                  return (
                                    <button
                                      key={lesson.id}
                                      onClick={() => handleSelectLesson(lesson.id)}
                                      className={`w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                                        isLessonActive
                                          ? 'bg-amber-100 border-notebook text-stone-900 shadow-2xs'
                                          : 'hover:bg-stone-100 text-stone-700'
                                      }`}
                                    >
                                      <div className="flex items-center gap-2 truncate">
                                        <BookOutlined
                                          className={`text-xs ${
                                            isLessonActive ? 'text-amber-800' : 'text-stone-400'
                                          }`}
                                        />
                                        <span className="truncate">{lesson.title}</span>
                                      </div>
                                      {lesson.status === 'Completed' && (
                                        <CheckCircleOutlined className="text-emerald-500 text-xs shrink-0" />
                                      )}
                                    </button>
                                  );
                                })}
                              </div>
                            )}
                          </div>
                        );
                      })
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Right Column: Lesson Details & Vocabulary */}
          <div className="lg:col-span-8 space-y-4">
            {selectedLesson ? (
              <div className="bg-white border-notebook-2 rounded-2xl p-6 shadow-xs space-y-6">
                {/* Lesson Header with Action Button & Prev/Next Navigator */}
                <div className="flex flex-wrap items-center justify-between gap-4 pb-4 border-b border-stone-200">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] font-mono uppercase tracking-wider text-rose-600 font-bold bg-rose-50 px-2 py-0.5 rounded border border-rose-200">
                        LESSON
                      </span>
                      {selectedCourse && (
                        <span className="text-[11px] font-mono text-stone-500 font-medium truncate max-w-xs">
                          {selectedCourse.units.find((u) =>
                            u.lessons.some((l) => l.id === selectedLesson.id)
                          )?.title}
                        </span>
                      )}
                    </div>
                    <h3 className="text-lg font-bold text-stone-900 mt-1">
                      {selectedLesson.title}
                    </h3>
                  </div>

                  <div className="flex items-center gap-2">
                    {!isAdmin && lessonCompleted && <span className="rounded-lg border-notebook bg-emerald-100 px-3 py-2 text-xs font-bold"><CheckCircleOutlined /> {t('courses.completed')}</span>}
                    {/* Previous Lesson Button */}
                    {prevLesson && (
                      <button
                        onClick={() => handleSelectLesson(prevLesson.id)}
                        className="px-2.5 py-2 bg-stone-100 text-stone-700 font-bold text-xs rounded-lg notebook-btn hover:bg-stone-200 transition cursor-pointer flex items-center gap-1"
                        title={prevLesson.title}
                      >
                        <LeftOutlined className="text-[10px]" />
                        <span className="hidden sm:inline">{t('courses.prevLesson')}</span>
                      </button>
                    )}

                    {/* Next Lesson Button */}
                    {nextLesson && (
                      <button
                        onClick={() => handleSelectLesson(nextLesson.id)}
                        className="px-2.5 py-2 bg-stone-100 text-stone-700 font-bold text-xs rounded-lg notebook-btn hover:bg-stone-200 transition cursor-pointer flex items-center gap-1"
                        title={nextLesson.title}
                      >
                        <span className="hidden sm:inline">{t('courses.nextLesson')}</span>
                        <RightOutlined className="text-[10px]" />
                      </button>
                    )}

                    {/* Quiz Button */}
                    <button
                      onClick={() => onStartQuiz(selectedLesson.id, selectedLesson.title)}
                      className="flex items-center gap-2 px-4 py-2 bg-amber-400 text-stone-900 font-bold text-xs rounded-lg notebook-btn hover:bg-amber-500 transition cursor-pointer"
                    >
                      <PlayCircleOutlined />
                      <span>{t('courses.practiceQuiz')}</span>
                    </button>
                  </div>
                </div>

                {/* Guided vocabulary practice is the primary learning action. */}
                {!isAdmin && <div className="rounded-xl border-notebook bg-amber-50 p-4">
                  <h4 className="text-sm font-bold">{t('courses.guidedVocabulary')}</h4>
                  <p className="mt-1 text-xs text-stone-600">{t('courses.guidedVocabularyDescription')}</p>
                  {vocabularyChoices.find((item) => item.lessonId === selectedLesson.id) ? <div className="mt-3 flex flex-wrap items-center gap-3">
                    <span className="text-xs text-stone-600">{t('courses.vocabularyGroups', {
                      completed: vocabularyChoices.find((item) => item.lessonId === selectedLesson.id)!.completedGroups,
                      total: vocabularyChoices.find((item) => item.lessonId === selectedLesson.id)!.availableGroups
                    })}</span>
                    <button type="button" onClick={() => setActiveVocabularyLessonId(selectedLesson.id)}
                      className="rounded-lg bg-amber-400 px-4 py-2 text-xs font-bold disabled:opacity-50"
                      disabled={vocabularyChoices.find((item) => item.lessonId === selectedLesson.id)!.completedGroups >= vocabularyChoices.find((item) => item.lessonId === selectedLesson.id)!.availableGroups}>
                      {t('courses.startVocabularyPractice')}
                    </button>
                  </div> : <p className="mt-3 text-xs text-rose-700">{t('courses.vocabularyPracticeUnavailable')}</p>}
                </div>}

                {/* Vocabulary reference */}
                <details>
                  <summary className="cursor-pointer text-xs font-bold text-stone-600">{t('courses.vocabularyReference')} ({selectedLesson.vocabularies?.length || 0})</summary>
                <div className="mt-4">
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="text-xs uppercase font-bold tracking-wider text-stone-500">
                      {t('courses.vocabularies')} ({selectedLesson.vocabularies?.length || 0})
                    </h4>
                    {selectedLesson.vocabularies?.length > 0 && (
                      <span className="text-[11px] text-stone-400 font-mono">
                         {language === 'vi' ? 'Bấm vào biểu tượng loa để nghe phát âm bản ngữ' : 'Select the speaker icon to hear the pronunciation'}
                      </span>
                    )}
                  </div>

                  {selectedLesson.vocabularies?.length > 0 ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {selectedLesson.vocabularies.map((vocab) => (
                        <div
                          key={vocab.id}
                          className="p-3.5 bg-[#FFF9E6] border-notebook rounded-xl relative hover:shadow-xs transition flex flex-col justify-between"
                        >
                          <div>
                            <div className="flex items-start justify-between gap-2">
                              <div>
                                <span className="font-bold text-base text-stone-900">
                                  {vocab.term}
                                </span>
                                {vocab.partOfSpeech && vocab.partOfSpeech !== 'vocabulary' && (
                                  <span className="ml-2 text-xs text-stone-500">{vocab.partOfSpeech}</span>
                                )}
                                {vocab.phonetic && (
                                  <span className="text-xs text-stone-500 block font-mono">
                                    {vocab.phonetic}
                                  </span>
                                )}
                              </div>
                              <button
                                onClick={() => speak(vocab.term)}
                                className={`p-1.5 rounded-lg border-notebook transition shrink-0 cursor-pointer ${
                                  speakingTerm === vocab.term
                                    ? 'bg-amber-400 text-stone-900 scale-105'
                                    : 'bg-white text-stone-600 hover:bg-stone-100'
                                }`}
                                title={t('courses.listenPronunciation')}
                              >
                                <SoundOutlined />
                              </button>
                            </div>

                            <div className="mt-2 text-xs text-stone-800 font-medium">
                              {(vocab.meanings?.length ? vocab.meanings : [vocab.meaning]).map((meaning, index) => (
                                <div key={index}>{index + 1}. {meaning}</div>
                              ))}
                            </div>
                          </div>

                          {vocab.example && (
                            <div className="mt-2.5 pt-2 border-t border-amber-200/80 text-[11px] text-stone-600 italic">
                              "{vocab.example}"
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="p-8 text-center text-stone-400 text-xs bg-stone-50 rounded-xl border border-dashed border-stone-300">
                       {language === 'vi' ? 'Chưa có từ vựng nào trong bài học này.' : 'This lesson has no vocabulary yet.'}
                    </div>
                  )}
                </div>
                </details>
              </div>
            ) : (
              <div className="bg-white border-notebook-2 rounded-2xl p-6">
                {currentMatrixUnit ? <>
                  <span className="text-xs font-bold uppercase tracking-wider text-amber-800">{t('courses.unitOverview')}</span>
                  <h2 className="mt-2 text-xl font-bold">{currentMatrixUnit.title}</h2>
                  <p className="mt-1 text-xs text-stone-600">{t('courses.unitLessonCount', { count: currentMatrixUnit.lessons.length })}</p>
                  <div className="mt-5 space-y-2">{currentMatrixUnit.lessons.map((lesson) => {
                    const choice = vocabularyChoices.find((item) => item.lessonId === lesson.id);
                    return <div key={lesson.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-stone-200 bg-[#FAF6EE] p-3">
                      <div className="min-w-0"><strong className="block text-sm">{lesson.title}</strong>
                        {choice && <span className="text-xs text-stone-600">{t('courses.vocabularyGroups', { completed: choice.completedGroups, total: choice.availableGroups })}</span>}</div>
                      <div className="flex gap-2">
                        <button type="button" onClick={() => void handleSelectLesson(lesson.id)} className="rounded-lg border-notebook bg-white px-3 py-2 text-xs font-bold">{t('courses.openLesson')}</button>
                        {!isAdmin && choice && choice.completedGroups < choice.availableGroups && <button type="button" onClick={() => setActiveVocabularyLessonId(lesson.id)} className="rounded-lg bg-amber-400 px-3 py-2 text-xs font-bold">{t('courses.startVocabularyPractice')}</button>}
                      </div>
                    </div>;
                  })}</div>
                </> : <p className="text-sm text-stone-600">{t('courses.startLesson')}</p>}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Course Grid (Course Selection Screen)
  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-xl font-bold text-stone-900">{t('courses.title')}</h1>
          <p className="text-xs text-stone-600">{t('courses.subtitle')}</p>
        </div>

        {/* Thanh Chọn Nhanh Bộ Lọc Ngôn Ngữ Khóa Học */}
        <div className="flex items-center gap-1 p-1 rounded-xl bg-stone-100 border border-stone-200">
          {[
            { code: 'all', name: t('common.all'), flagEmoji: '' },
            ...languages.filter((item) => courses.some((course) => course.languageId === item.id))
          ].map((item) => (
            <button
              key={item.code}
              onClick={() => setLanguageFilter(item.code)}
              className={`px-3 py-1.5 rounded-lg text-xs font-bold transition flex items-center gap-1.5 cursor-pointer ${
                languageFilter === item.code
                  ? 'bg-white shadow-xs text-stone-900 border border-stone-300'
                  : 'text-stone-600 hover:text-stone-900'
              }`}
            >
              {item.flagEmoji && <span>{item.flagEmoji}</span>}
              <span>{item.code === 'en' ? t('common.english') : item.code === 'vi' ? t('common.vietnamese') : item.name}</span>
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {filteredCourses.length === 0 ? (
          <div className="col-span-full p-12 text-center text-xs text-stone-500 bg-white rounded-2xl border-notebook-2">
            {target || isAdmin ? t('courses.noCoursesForTarget') : t('courses.selectTarget')}
          </div>
        ) : (
          filteredCourses.map((course) => (
            <div
              key={course.id}
              className="bg-white border-notebook-2 rounded-2xl p-5 hover:shadow-md transition flex flex-col justify-between"
            >
              <div>
                <div className="flex items-center justify-between mb-3">
                  <span className="text-xs font-mono font-bold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800 border border-emerald-300">
                    {course.level}
                  </span>
                  <span className="text-xs text-stone-500 font-medium">
                    {course.totalLessons} {t('courses.lessons')}
                  </span>
                </div>

                <h2 className="text-base font-bold text-stone-900 mb-2 leading-snug">
                  {course.title}
                </h2>
                <p className="text-xs text-stone-600 line-clamp-3 mb-4 leading-relaxed">
                  {course.description}
                </p>
              </div>

              <button
                onClick={() => handleSelectCourse(course.id)}
                className="w-full py-2 bg-stone-900 text-white rounded-lg text-xs font-bold notebook-btn hover:bg-stone-800 transition cursor-pointer"
              >
                {t('courses.startLesson')}
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
