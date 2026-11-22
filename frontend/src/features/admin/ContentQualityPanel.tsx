import { useState } from 'react';
import type { Course } from '../../types';
import { coursesApi, getErrorMessage } from '../../services/apiClient';
import { Pagination } from '../../components/ui/Pagination';
import { useTranslation } from '../../i18n';

interface Issue { courseId: string; course: string; location: string; message: string }
const sections: Record<string, string> = {
  flashcards: 'vocabulary', grammarTopics: 'grammar', listeningExercises: 'listening',
  speakingExercises: 'speaking', readingArticles: 'reading', writingPrompts: 'writing',
  quizQuestions: 'quiz'
};
const object = (value: unknown): Record<string, unknown> | null =>
  value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : null;

function inspect(course: Course, material: unknown): Issue[] {
  const issues: Issue[] = [];
  const add = (location: string, message: string) => issues.push({ courseId: course.id, course: course.title, location, message });
  const root = object(material);
  if (!root || !Array.isArray(root.units)) { add('Course', 'Missing or invalid course material'); return issues; }
  for (const [unitIndex, rawUnit] of root.units.entries()) {
    const unit = object(rawUnit);
    const unitName = String(unit?.title || `Unit ${unitIndex + 1}`);
    if (!unit || !Array.isArray(unit.lessons) || !unit.lessons.length) { add(unitName, 'No lessons'); continue; }
    for (const [lessonIndex, rawLesson] of unit.lessons.entries()) {
      const lesson = object(rawLesson);
      const location = `${unitName} / ${String(lesson?.title || `Lesson ${lessonIndex + 1}`)}`;
      if (!lesson) { add(location, 'Invalid lesson'); continue; }
      if (!String(lesson.summary || '').trim()) add(location, 'Missing lesson objective or context');
      if (!Array.isArray(lesson.lessonSteps) || !lesson.lessonSteps.length) {
        add(location, 'No explicit lessonSteps; the player uses inferred order');
      } else {
        const types = new Set(lesson.lessonSteps.map(step => object(step)?.type));
        if (!types.has('introduction')) add(location, 'Missing introduction step');
        for (const [section, stepType] of Object.entries(sections))
          if (Array.isArray(lesson[section]) && lesson[section].length && !types.has(stepType))
            add(location, `Content in ${section} has no lesson step`);
      }
      for (const [wordIndex, rawWord] of (Array.isArray(lesson.flashcards) ? lesson.flashcards : []).entries()) {
        const word = object(rawWord);
        if (!word) continue;
        if (!String(word.example || '').trim() || !String(word.exampleTranslation || '').trim())
          add(`${location} / word ${wordIndex + 1}`, 'Missing example and translation');
        if (!String(word.clozeSentence || '').includes('___'))
          add(`${location} / word ${wordIndex + 1}`, 'Missing contextual cloze sentence');
      }
    }
  }
  return issues;
}

export function ContentQualityPanel({ courses }: { courses: Course[] }) {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const [issues, setIssues] = useState<Issue[]>([]);
  const [scanned, setScanned] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);
  const scan = async () => {
    setLoading(true); setError('');
    const found: Issue[] = [];
    for (let start = 0; start < courses.length; start += 5) {
      const batch = await Promise.allSettled(courses.slice(start, start + 5).map(async course =>
        inspect(course, await coursesApi.getCourseMaterial(course.id))));
      batch.forEach((result, index) => {
        if (result.status === 'fulfilled') found.push(...result.value);
        else {
          const course = courses[start + index];
          found.push({ courseId: course.id, course: course.title, location: 'Course', message: getErrorMessage(result.reason, 'Could not read material') });
        }
      });
    }
    setIssues(found); setScanned(true); setPage(1); setLoading(false);
  };
  const pageCount = Math.max(1, Math.ceil(issues.length / 20));
  return <section className="space-y-4 rounded-2xl border-notebook bg-white p-5">
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div><h2 className="text-lg font-bold">{vi ? 'Chất lượng nội dung bài học' : 'Lesson content quality'}</h2>
        <p className="text-xs text-stone-600">{vi ? 'Quét mục tiêu, các bước học và ví dụ từ vựng trong khóa học hiện tại.' : 'Check objectives, lesson steps and vocabulary examples in current courses.'}</p></div>
      <button type="button" disabled={loading} onClick={() => void scan()} className="rounded-lg bg-amber-300 px-4 py-2 text-xs font-bold disabled:opacity-50">{loading ? (vi ? 'Đang quét...' : 'Scanning...') : (vi ? 'Quét nội dung' : 'Scan content')}</button>
    </div>
    {error && <p role="alert" className="text-sm text-rose-700">{error}</p>}
    {scanned && <p className="text-sm font-bold">{vi ? `${issues.length} mục cần xem lại trong ${courses.length} khóa học` : `${issues.length} items to review across ${courses.length} courses`}</p>}
    {scanned && issues.length === 0 && <p className="rounded-xl bg-emerald-50 p-4 text-sm">{vi ? 'Không tìm thấy mục thiếu trong các quy tắc đã quét.' : 'No issues found by these checks.'}</p>}
    {issues.slice((page - 1) * 20, page * 20).map((issue, index) => <div key={`${issue.courseId}-${issue.location}-${issue.message}-${index}`} className="rounded-lg border border-stone-200 p-3 text-sm">
      <strong>{issue.course}</strong><span className="mx-2 text-stone-400">/</span><span>{issue.location}</span>
      <p className="mt-1 text-xs text-amber-800">{issue.message}</p>
    </div>)}
    <Pagination page={page} pageCount={pageCount} onChange={setPage} />
  </section>;
}
