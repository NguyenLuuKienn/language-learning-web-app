import { useEffect, useRef, useState } from 'react';
import { App } from 'antd';
import type { CourseDetail, Language } from '../../types';
import { adminApi, getErrorMessage } from '../../services/apiClient';
import { createCourseMaterialTemplate, type CourseMaterial } from '../../services/courseMaterial';

type PlacementTests = CourseMaterial['placementTests'];
type PlacementQuestion = PlacementTests[number]['questions'][number];

interface Props {
  course: CourseDetail;
  language: Language;
  onClose: () => void;
}

const skills = ['listening', 'speaking', 'reading', 'writing'] as const;

export function PlacementEditor({ course, language, onClose }: Props) {
  const { message } = App.useApp();
  const inputRef = useRef<HTMLInputElement>(null);
  const [tests, setTests] = useState<PlacementTests>(() => createCourseMaterialTemplate(course, language).placementTests);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let active = true;
    adminApi.getPlacementTests(course.id).then((data) => {
      if (active && data.length > 0) setTests(data);
    }).catch((error) => {
      if (active) message.error(getErrorMessage(error, 'Không tải được đề đầu vào.'));
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [course.id, message]);

  const updateQuestion = (testIndex: number, questionIndex: number, changes: Partial<PlacementQuestion>) => {
    setTests((current) => current.map((test, index) => index === testIndex ? {
      ...test,
      questions: test.questions.map((question, position) => position === questionIndex ? { ...question, ...changes } : question)
    } : test));
  };

  const addQuestion = (testIndex: number, section: PlacementQuestion['section']) => {
    const template = createCourseMaterialTemplate(course, language).placementTests[0].questions.find((question) => question.section === section);
    if (!template) return;
    setTests((current) => current.map((test, index) => index === testIndex ? {
      ...test, questions: [...test.questions, { ...template }]
    } : test));
  };


  const download = () => {
    const blob = new Blob([JSON.stringify(tests, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `placement_${language.code}_${course.level}.json`;
    link.click();
    URL.revokeObjectURL(url);
  };

  const importJson = async (file?: File) => {
    if (!file) return;
    try {
      const parsed: unknown = JSON.parse(await file.text());
      const candidate = Array.isArray(parsed) ? parsed : (parsed as { placementTests?: unknown })?.placementTests;
      if (!Array.isArray(candidate) || !candidate.every((test) => test?.level === course.level && Array.isArray(test.questions)))
        throw new Error('JSON cần mảng placementTests đúng trình độ khóa học.');
      setTests(candidate as PlacementTests);
      message.success('Đã nạp JSON. Bấm Lưu để cập nhật đề.');
    } catch (error) {
      message.error(getErrorMessage(error, 'JSON không hợp lệ.'));
    }
  };

  const save = async () => {
    setSaving(true);
    try {
      await adminApi.updatePlacementTests(course.id, tests);
      message.success('Đã lưu đề đầu vào.');
      onClose();
    } catch (error) {
      message.error(getErrorMessage(error, 'Không lưu được đề đầu vào.'));
    } finally {
      setSaving(false);
    }
  };

  return <div className="fixed inset-0 z-[60] overflow-y-auto bg-black/60 p-4" role="dialog" aria-modal="true" aria-label="Quản lý đề đầu vào">
    <div className="mx-auto my-8 max-w-5xl rounded-2xl border-2 border-stone-900 bg-[#FAF6EE] p-5">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div><h2 className="text-lg font-bold">Đề đầu vào: {course.title}</h2><p className="text-sm text-stone-600">{language.name} · {course.level} · {course.domain}</p></div>
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={download} className="rounded border border-stone-900 px-3 py-2">Tải JSON mẫu / hiện tại</button>
          <button type="button" onClick={() => inputRef.current?.click()} className="rounded border border-stone-900 px-3 py-2">Nhập JSON</button>
          <input ref={inputRef} type="file" accept="application/json,.json" className="hidden" onChange={(event) => { void importJson(event.target.files?.[0]); event.target.value = ''; }} />
          <button type="button" onClick={onClose} className="rounded border border-stone-900 px-3 py-2">Đóng</button>
        </div>
      </div>
      {loading ? <p>Đang tải đề...</p> : <div className="max-h-[65vh] space-y-5 overflow-y-auto pr-2">
        {tests.map((test, testIndex) => <section key={testIndex} className="rounded-xl border border-stone-300 bg-white p-4">
          <h3 className="mb-3 font-bold">Bài {testIndex + 1}: {test.level} · {test.questions.length} câu</h3>
          {skills.map((skill) => <div key={skill} className="mb-4">
            <div className="mb-2 flex items-center justify-between"><h4 className="font-semibold capitalize">{skill}</h4><button type="button" onClick={() => addQuestion(testIndex, skill)} className="text-sm underline">Thêm câu</button></div>
            {test.questions.map((question, questionIndex) => question.section === skill && <div key={questionIndex} className="mb-2 grid gap-2 rounded-lg border border-stone-200 p-3 sm:grid-cols-2">
              <label className="sm:col-span-2">Câu hỏi<textarea value={question.prompt} onChange={(event) => updateQuestion(testIndex, questionIndex, { prompt: event.target.value })} className="mt-1 w-full rounded border p-2" /></label>
              {skill === 'listening' && <label className="sm:col-span-2">Lời thoại nghe<textarea value={question.audioScript ?? ''} onChange={(event) => updateQuestion(testIndex, questionIndex, { audioScript: event.target.value })} className="mt-1 w-full rounded border p-2" /></label>}
              {skill === 'reading' && <label className="sm:col-span-2">Đoạn đọc<textarea value={question.readingPassage ?? ''} onChange={(event) => updateQuestion(testIndex, questionIndex, { readingPassage: event.target.value })} className="mt-1 w-full rounded border p-2" /></label>}
              {(skill === 'listening' || skill === 'reading') && <>
                <label>Lựa chọn, mỗi dòng một đáp án<textarea value={(question.options ?? []).join('\n')} onChange={(event) => updateQuestion(testIndex, questionIndex, { options: event.target.value.split('\n') })} className="mt-1 w-full rounded border p-2" /></label>
                <label>Đáp án đúng (bắt đầu từ 1)<input type="number" min={1} value={(question.correctIndex ?? 0) + 1} onChange={(event) => updateQuestion(testIndex, questionIndex, { correctIndex: Number(event.target.value) - 1 })} className="mt-1 w-full rounded border p-2" /></label>
              </>}
              {(skill === 'speaking' || skill === 'writing') && <label className="sm:col-span-2">Bài mẫu / tiêu chí nội dung<textarea value={question.expectedResponse ?? ''} onChange={(event) => updateQuestion(testIndex, questionIndex, { expectedResponse: event.target.value })} className="mt-1 w-full rounded border p-2" /></label>}
              <label>Thang điểm<input type="number" min={1} max={100} value={question.points} onChange={(event) => updateQuestion(testIndex, questionIndex, { points: Number(event.target.value) })} className="mt-1 w-full rounded border p-2" /></label>
              {skill === 'writing' && <label>Số từ tối thiểu<input type="number" min={1} max={500} value={question.minWords ?? 30} onChange={(event) => updateQuestion(testIndex, questionIndex, { minWords: Number(event.target.value) })} className="mt-1 w-full rounded border p-2" /></label>}
              <label className="sm:col-span-2">Rubric<textarea value={question.rubric} onChange={(event) => updateQuestion(testIndex, questionIndex, { rubric: event.target.value })} className="mt-1 w-full rounded border p-2" /></label>
              <button type="button" onClick={() => setTests((current) => current.map((item, index) => index === testIndex ? { ...item, questions: item.questions.filter((_, position) => position !== questionIndex) } : item))} className="text-left text-red-700 underline">Xóa câu</button>
            </div>)}
          </div>)}
        </section>)}
      </div>}
      <div className="mt-4 flex justify-end"><button type="button" disabled={loading || saving} onClick={() => void save()} className="rounded bg-stone-900 px-5 py-2 font-semibold text-white disabled:opacity-50">{saving ? 'Đang lưu...' : 'Lưu đề đầu vào'}</button></div>
    </div>
  </div>;
}
