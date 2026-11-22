import { useEffect, useState } from 'react';
import { App } from 'antd';
import { useTranslation } from '../../i18n';
import type { CurriculumLesson } from '../../services/curriculumStore';
import { adminApi, coursesApi, getErrorMessage, lessonJourneyApi } from '../../services/apiClient';
import type { CourseMaterial } from '../../services/courseMaterial';

type SectionKey = 'flashcards' | 'listeningExercises' | 'speakingExercises' | 'readingArticles' | 'writingPrompts' | 'grammarTopics' | 'quizQuestions';
type ContentItem = Record<string, unknown>;
type SectionState = Record<SectionKey, ContentItem[]>;
type Field = { key: string; en: string; vi: string; kind?: 'lines' | 'number' | 'vocabulary' };

const sectionKeys: SectionKey[] = ['flashcards', 'listeningExercises', 'speakingExercises', 'readingArticles', 'writingPrompts', 'grammarTopics', 'quizQuestions'];
const labels: Record<SectionKey, { en: string; vi: string }> = {
  flashcards: { en: 'Vocabulary / flashcards', vi: 'Từ vựng / flashcard' },
  listeningExercises: { en: 'Listening', vi: 'Luyện nghe' },
  speakingExercises: { en: 'Speaking / pronunciation', vi: 'Nói / phát âm' },
  readingArticles: { en: 'Reading', vi: 'Đọc hiểu' },
  writingPrompts: { en: 'Writing', vi: 'Luyện viết' },
  grammarTopics: { en: 'Grammar', vi: 'Ngữ pháp' },
  quizQuestions: { en: 'Quiz', vi: 'Câu hỏi quiz' }
};
const fields: Record<SectionKey, Field[]> = {
  flashcards: [
    { key: 'front', en: 'Front', vi: 'Mặt trước' }, { key: 'back', en: 'Back', vi: 'Mặt sau' },
    { key: 'phonetic', en: 'Phonetic', vi: 'Phiên âm' }, { key: 'partOfSpeech', en: 'Part of speech', vi: 'Từ loại' },
    { key: 'meanings', en: 'Meanings, one per line', vi: 'Các nghĩa, mỗi dòng một nghĩa', kind: 'lines' },
    { key: 'example', en: 'Example', vi: 'Ví dụ' },
    { key: 'audioUrl', en: 'Audio URL', vi: 'Đường dẫn âm thanh' },
    { key: 'imageUrl', en: 'Image URL', vi: 'Đường dẫn hình ảnh' }
  ],
  listeningExercises: [
    { key: 'title', en: 'Title', vi: 'Tiêu đề' }, { key: 'audioScript', en: 'Audio script', vi: 'Nội dung nghe' },
    { key: 'question', en: 'Question', vi: 'Câu hỏi' }, { key: 'options', en: 'Options, one per line', vi: 'Đáp án, mỗi dòng một lựa chọn', kind: 'lines' },
    { key: 'correctIndex', en: 'Correct option (starting at 1)', vi: 'Đáp án đúng (bắt đầu từ 1)', kind: 'number' },
    { key: 'explanation', en: 'Explanation', vi: 'Giải thích' }
  ],
  speakingExercises: [
    { key: 'title', en: 'Title', vi: 'Tiêu đề' }, { key: 'phrase', en: 'Practice phrase', vi: 'Câu luyện nói' },
    { key: 'phonetic', en: 'Phonetic', vi: 'Phiên âm' }, { key: 'translation', en: 'Translation', vi: 'Bản dịch' },
    { key: 'context', en: 'Context', vi: 'Ngữ cảnh' }
  ],
  readingArticles: [
    { key: 'title', en: 'Title', vi: 'Tiêu đề' }, { key: 'content', en: 'Paragraphs, one per line', vi: 'Đoạn văn, mỗi dòng một đoạn', kind: 'lines' },
    { key: 'vocabularies', en: 'Vocabulary: word | meaning', vi: 'Từ vựng: từ | nghĩa', kind: 'vocabulary' },
    { key: 'quiz.question', en: 'Reading question', vi: 'Câu hỏi đọc hiểu' },
    { key: 'quiz.options', en: 'Options, one per line', vi: 'Đáp án, mỗi dòng một lựa chọn', kind: 'lines' },
    { key: 'quiz.correctIndex', en: 'Correct option (starting at 1)', vi: 'Đáp án đúng (bắt đầu từ 1)', kind: 'number' }
  ],
  writingPrompts: [
    { key: 'title', en: 'Title', vi: 'Tiêu đề' }, { key: 'scenario', en: 'Scenario', vi: 'Tình huống' },
    { key: 'instruction', en: 'Instruction', vi: 'Hướng dẫn' }, { key: 'minWords', en: 'Minimum words', vi: 'Số từ tối thiểu', kind: 'number' }
  ],
  grammarTopics: [
    { key: 'title', en: 'Title', vi: 'Tiêu đề' }, { key: 'formula', en: 'Structure', vi: 'Cấu trúc' },
    { key: 'explanation', en: 'Explanation', vi: 'Giải thích' }, { key: 'example', en: 'Example', vi: 'Ví dụ' },
    { key: 'exampleTranslation', en: 'Example translation', vi: 'Dịch ví dụ' }, { key: 'question', en: 'Practice question', vi: 'Câu hỏi luyện tập' },
    { key: 'options', en: 'Options, one per line', vi: 'Đáp án, mỗi dòng một lựa chọn', kind: 'lines' },
    { key: 'correctIndex', en: 'Correct option (starting at 1)', vi: 'Đáp án đúng (bắt đầu từ 1)', kind: 'number' }
  ],
  quizQuestions: [
    { key: 'question', en: 'Question', vi: 'Câu hỏi' }, { key: 'options', en: 'Options, one per line', vi: 'Đáp án, mỗi dòng một lựa chọn', kind: 'lines' },
    { key: 'correctIndex', en: 'Correct option (starting at 1)', vi: 'Đáp án đúng (bắt đầu từ 1)', kind: 'number' },
    { key: 'explanation', en: 'Explanation', vi: 'Giải thích' }
  ]
};

const emptySections = (): SectionState => Object.fromEntries(sectionKeys.map((key) => [key, []])) as unknown as SectionState;

function newItem(section: SectionKey): ContentItem {
  const item: ContentItem = { id: crypto.randomUUID() };
  for (const field of fields[section]) {
    if (field.key.includes('.')) continue;
    item[field.key] = field.kind === 'lines' || field.kind === 'vocabulary' ? [] : field.kind === 'number' ? 0 : '';
  }
  if (section === 'readingArticles') item.quiz = { question: '', options: ['', ''], correctIndex: 0 };
  if (section === 'quizQuestions' || section === 'listeningExercises') item.options = ['', ''];
  if (section === 'writingPrompts') item.minWords = 30;
  return item;
}

function fieldValue(item: ContentItem, field: Field): string {
  const [root, child] = field.key.split('.');
  const value = child ? (item[root] as ContentItem | undefined)?.[child] : item[root];
  if (field.kind === 'number') return String(field.key.endsWith('correctIndex') ? Number(value ?? 0) + 1 : value ?? 0);
  if (field.kind === 'vocabulary') return Array.isArray(value) ? value.map((entry: { word: string; meaning: string }) => `${entry.word} | ${entry.meaning}`).join('\n') : '';
  return Array.isArray(value) ? value.join('\n') : String(value ?? '');
}

function changedField(item: ContentItem, field: Field, input: string): ContentItem {
  let value: unknown = input;
  if (field.kind === 'number') value = field.key.endsWith('correctIndex') ? Number(input) - 1 : Number(input);
  if (field.kind === 'lines') value = input.split('\n').map((line) => line.trim()).filter(Boolean);
  if (field.kind === 'vocabulary') value = input.split('\n').filter((line) => line.trim()).map((line) => {
    const [word, ...meaning] = line.split('|');
    return { word: word.trim(), meaning: meaning.join('|').trim() };
  });
  const [root, child] = field.key.split('.');
  return child ? { ...item, [root]: { ...(item[root] as ContentItem), [child]: value } } : { ...item, [root]: value };
}

interface Props {
  courseId: string;
  lessonId: string;
  onClose: () => void;
  onSaved: () => void;
}

export function LessonMaterialEditor({ courseId, lessonId, onClose, onSaved }: Props) {
  const { message } = App.useApp();
  const { language } = useTranslation();
  const [lesson, setLesson] = useState<CurriculumLesson | null>(null);
  const [sections, setSections] = useState<SectionState>(emptySections);
  const [active, setActive] = useState<SectionKey>('flashcards');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [sourceStepId, setSourceStepId] = useState('');
  const [targetStepId, setTargetStepId] = useState('');
  const [remapping, setRemapping] = useState(false);

  useEffect(() => {
    let mounted = true;
    coursesApi.getCourseMaterial(courseId).then((data) => {
      const material = data as CourseMaterial | null;
      const found = material?.units.flatMap((unit) => unit.lessons).find((item) => item.id === lessonId);
      if (!found) throw new Error(language === 'vi' ? 'Bài học chưa có nội dung để sửa.' : 'This lesson has no editable material.');
      if (!mounted) return;
      setLesson(found);
      setSections(Object.fromEntries(sectionKeys.map((key) => [key, (found[key] ?? []).map((item) => ({ ...item }))])) as unknown as SectionState);
    }).catch((error) => { if (mounted) message.error(getErrorMessage(error, 'Could not load lesson material.')); })
      .finally(() => { if (mounted) setLoading(false); });
    return () => { mounted = false; };
  }, [courseId, lessonId, language, message]);

  const update = (index: number, field: Field, value: string) => {
    setSections((current) => ({ ...current, [active]: current[active].map((item, position) => position === index ? changedField(item, field, value) : item) }));
  };

  const save = async () => {
    if (!lesson) return;
    setSaving(true);
    try {
      await adminApi.updateLessonMaterial(courseId, lessonId, { ...lesson, ...sections } as unknown as CurriculumLesson);
      message.success(language === 'vi' ? 'Đã lưu nội dung bài học.' : 'Lesson material saved.');
      onSaved();
      onClose();
    } catch (error) {
      message.error(getErrorMessage(error, language === 'vi' ? 'Không lưu được bài học.' : 'Could not save lesson.'));
    } finally {
      setSaving(false);
    }
  };

  const remapStep = async () => {
    if (!lesson || !sourceStepId || !targetStepId.trim()) return;
    setRemapping(true);
    try {
      const journey = await lessonJourneyApi.get(lessonId);
      await adminApi.remapLessonSteps(courseId, lessonId, journey.contentVersion, { [sourceStepId]: targetStepId.trim() });
      message.success(language === 'vi' ? 'Đã chuyển ID bước và tiến độ liên quan.' : 'Step ID and related progress migrated.');
      onSaved();
      onClose();
    } catch (error) {
      message.error(getErrorMessage(error, language === 'vi' ? 'Không chuyển được ID bước.' : 'Could not migrate step ID.'));
    } finally {
      setRemapping(false);
    }
  };

  return <div className="fixed inset-0 z-[70] overflow-y-auto bg-black/60 p-4" role="dialog" aria-modal="true" aria-label={language === 'vi' ? 'Sửa nội dung bài học' : 'Edit lesson material'}>
    <div className="mx-auto my-6 max-w-5xl rounded-2xl border-2 border-stone-900 bg-[#FAF6EE] p-5">
      <div className="mb-4 flex items-center justify-between gap-3 border-b border-stone-300 pb-3">
        <div><h2 className="text-lg font-bold">{language === 'vi' ? 'Sửa bài học' : 'Edit lesson'}: {lesson?.title}</h2><p className="text-sm text-stone-600">{language === 'vi' ? 'Chọn từng phần, sửa trực tiếp rồi lưu.' : 'Select a section, edit its entries, then save.'}</p></div>
        <button type="button" onClick={onClose} className="rounded border px-3 py-1">{language === 'vi' ? 'Đóng' : 'Close'}</button>
      </div>
      {loading ? <p>{language === 'vi' ? 'Đang tải...' : 'Loading...'}</p> : lesson ? <>
        <div className="mb-4 flex flex-wrap gap-2">{sectionKeys.map((key) => <button key={key} type="button" onClick={() => setActive(key)} className={`rounded-lg border px-3 py-2 text-sm ${active === key ? 'border-stone-900 bg-stone-900 text-white' : 'border-stone-300 bg-white'}`}>{labels[key][language]} ({sections[key].length})</button>)}</div>
        <div className="max-h-[56vh] space-y-3 overflow-y-auto pr-1">{sections[active].map((item, index) => <section key={String(item.id ?? index)} className="rounded-xl border border-stone-300 bg-white p-4">
          <div className="mb-3 flex justify-between"><h3 className="font-semibold">{labels[active][language]} {index + 1}</h3><button type="button" onClick={() => setSections((current) => ({ ...current, [active]: current[active].filter((_, position) => position !== index) }))} className="text-red-700">{language === 'vi' ? 'Xóa' : 'Remove'}</button></div>
          <div className="grid gap-3 sm:grid-cols-2">{fields[active].map((field) => <label key={field.key} className="text-sm">{field[language]}
            {field.kind === 'number' ? <input type="number" min={field.key.endsWith('correctIndex') ? 1 : 0} value={fieldValue(item, field)} onChange={(event) => update(index, field, event.target.value)} className="mt-1 w-full rounded border border-stone-300 p-2" />
              : <textarea rows={field.kind === 'lines' || field.key === 'content' ? 4 : 2} value={fieldValue(item, field)} onChange={(event) => update(index, field, event.target.value)} className="mt-1 w-full rounded border border-stone-300 p-2" />}</label>)}</div>
        </section>)}</div>
        {lesson.lessonSteps?.length ? <details className="mt-4 rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm">
          <summary className="cursor-pointer font-semibold">{language === 'vi' ? 'Chuyển ID bước học có tiến độ' : 'Migrate a step ID with learner progress'}</summary>
          <p className="mt-2 text-stone-700">{language === 'vi' ? 'Chỉ đổi ID bước. Nội dung và điều kiện hoàn thành giữ nguyên; tiến độ được chuyển cùng lúc.' : 'Change only the step ID. Content and completion rules stay the same; progress moves together.'}</p>
          <div className="mt-3 flex flex-wrap gap-2">
            <select aria-label={language === 'vi' ? 'Bước hiện tại' : 'Current step'} value={sourceStepId} onChange={(event) => setSourceStepId(event.target.value)} className="rounded border border-stone-300 bg-white px-3 py-2">
              <option value="">{language === 'vi' ? 'Chọn bước' : 'Select step'}</option>
              {lesson.lessonSteps.map((step) => <option key={step.id} value={step.id}>{step.title} ({step.id})</option>)}
            </select>
            <input aria-label={language === 'vi' ? 'ID bước mới' : 'New step ID'} value={targetStepId} onChange={(event) => setTargetStepId(event.target.value)} maxLength={100} placeholder={language === 'vi' ? 'ID bước mới' : 'New step ID'} className="rounded border border-stone-300 px-3 py-2" />
            <button type="button" disabled={remapping || !sourceStepId || !targetStepId.trim()} onClick={() => void remapStep()} className="rounded bg-amber-500 px-4 py-2 font-semibold disabled:opacity-50">{remapping ? '...' : language === 'vi' ? 'Chuyển ID' : 'Migrate ID'}</button>
          </div>
        </details> : null}
        <div className="mt-4 flex justify-between gap-3 border-t border-stone-300 pt-4"><button type="button" onClick={() => setSections((current) => ({ ...current, [active]: [...current[active], newItem(active)] }))} className="rounded border border-stone-900 bg-white px-4 py-2 font-semibold">+ {language === 'vi' ? 'Thêm mục' : 'Add item'}</button><button type="button" disabled={saving} onClick={() => void save()} className="rounded bg-stone-900 px-5 py-2 font-semibold text-white disabled:opacity-50">{saving ? (language === 'vi' ? 'Đang lưu...' : 'Saving...') : (language === 'vi' ? 'Lưu thay đổi' : 'Save changes')}</button></div>
      </> : null}
    </div>
  </div>;
}
