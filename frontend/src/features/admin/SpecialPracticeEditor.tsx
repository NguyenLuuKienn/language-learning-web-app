import { useEffect, useState } from 'react';
import { DownloadOutlined, UploadOutlined } from '@ant-design/icons';
import { adminApi, getErrorMessage } from '../../services/apiClient';
import { createSpecialPracticeTemplate } from '../../services/specialPracticeTemplate';
import { useTranslation } from '../../i18n';
import { SpecialPracticeQuestionEditor } from './SpecialPracticeQuestionEditor';
import type { CourseDetail } from '../../types';

export type SpecialPracticeSection = 'IELTS' | 'TOEIC' | 'interview';
interface Props { course: CourseDetail; onClose: () => void; initialSection?: SpecialPracticeSection }

export function SpecialPracticeEditor({ course, onClose, initialSection = 'IELTS' }: Props) {
  const { language } = useTranslation();
  const vi = language === 'vi';
  const [value, setValue] = useState(JSON.stringify({ certificateExams: [], interviewScenarios: [] }, null, 2));
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [section, setSection] = useState<SpecialPracticeSection>(initialSection);
  const kind = section === 'interview' ? 'interview' : 'certificate';
  const visible = (entries: Record<string, unknown>[]) => section === 'interview' ? entries : entries.filter(entry => entry.certificate === section);
  const [items, setItems] = useState<Record<string, unknown>[]>([]);
  const [draft, setDraft] = useState<Record<string, unknown> | null>(null);
  const [details, setDetails] = useState('[]');
  const [showImport, setShowImport] = useState(false);

  useEffect(() => {
    let active = true;
    adminApi.listSpecialPractice(course.id, kind).then((entries) => {
      if (!active) return;
      setItems(visible(entries));
      setDraft(null);
    }).catch((cause) => { if (active) setError(getErrorMessage(cause, vi ? 'Không tải được nội dung.' : 'Could not load content.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [course.id, kind, section, vi]);

  const download = () => {
    const template = createSpecialPracticeTemplate(course);
    const data = kind === 'certificate' ? { certificateExams: (template.certificateExams ?? []).filter(item => item.certificate === section) } : { interviewScenarios: template.interviewScenarios };
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url; link.download = `special_practice_${section.toLowerCase()}_${course.level}_${course.domain || 'general'}.json`;
    link.click(); URL.revokeObjectURL(url);
  };

  const edit = (item?: Record<string, unknown>) => {
    const template = createSpecialPracticeTemplate(course);
    const example = kind === 'certificate' ? template.certificateExams?.find(item => item.certificate === section) : template.interviewScenarios?.[0];
    const next = item ?? JSON.parse(JSON.stringify({ ...example, id: crypto.randomUUID(), title: '', level: course.level }),
      (key, value) => key === 'id' ? crypto.randomUUID() : value) as Record<string, unknown>;
    setDraft(kind === 'certificate' ? { ...next, certificate: section } : { ...next });
    setDetails(JSON.stringify(next[kind === 'certificate' ? 'sections' : 'turns'] ?? [], null, 2));
    setShowImport(false); setError('');
  };

  const saveItem = async () => {
    if (!draft) return;
    setSaving(true); setError('');
    try {
      const body: Record<string, unknown> = { ...draft, ...(kind === 'certificate' ? { certificate: section } : {}), [kind === 'certificate' ? 'sections' : 'turns']: JSON.parse(details) };
      const id = String(body.id);
      if (items.some(item => item.id === id)) await adminApi.editSpecialPractice(course.id, kind, id, body);
      else await adminApi.createSpecialPractice(course.id, kind, body);
      setItems(visible(await adminApi.listSpecialPractice(course.id, kind))); setDraft(null);
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không lưu được nội dung.' : 'Could not save content.')); }
    finally { setSaving(false); }
  };

  const removeItem = async (item: Record<string, unknown>) => {
    if (!window.confirm(vi ? `Xóa “${item.title}”?` : `Delete “${item.title}”?`)) return;
    setSaving(true); setError('');
    try { await adminApi.deleteSpecialPractice(course.id, kind, String(item.id));
      setItems(previous => previous.filter(entry => entry.id !== item.id));
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không xóa được nội dung.' : 'Could not delete content.')); }
    finally { setSaving(false); }
  };

  const save = async () => {
    setSaving(true); setError('');
    try {
      const content: unknown = JSON.parse(value);
      const key = kind === 'certificate' ? 'certificateExams' : 'interviewScenarios';
      const incoming = Array.isArray(content) ? content : content && typeof content === 'object' ? (content as Record<string, unknown>)[key] : null;
      if (!Array.isArray(incoming)) throw new Error(vi ? `JSON cần mảng ${key}.` : `JSON needs a ${key} array.`);
      if (kind === 'certificate' && incoming.some(item => !item || typeof item !== 'object' || item.certificate !== section)) {
        throw new Error(vi ? `Mục ${section} chỉ nhận đề có certificate: "${section}".` : `${section} accepts only exams with certificate: "${section}".`);
      }
      await adminApi.importSpecialPractice(course.id, kind, incoming);
      setItems(visible(await adminApi.listSpecialPractice(course.id, kind))); setShowImport(false);
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không lưu được nội dung.' : 'Could not save content.')); }
    finally { setSaving(false); }
  };

  return <div className="fixed inset-0 z-[70] flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true" aria-label={vi ? 'Quản lý luyện thi và phỏng vấn' : 'Manage certificate and interview practice'}>
    <section className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-2xl border-notebook-2 bg-white p-5">
      <div className="flex items-start justify-between gap-3"><div><h2 className="text-lg font-bold">{vi ? 'Luyện thi & phỏng vấn' : 'Exams & interviews'}</h2>
        <p className="mt-1 text-xs text-stone-600">{course.title} · {course.level} · {course.domain}</p></div>
        <button type="button" onClick={onClose} className="rounded-lg border-notebook px-3 py-2 text-xs">{vi ? 'Đóng' : 'Close'}</button></div>
      <div className="mt-4 flex gap-2" role="tablist" aria-label={vi ? 'Loại nội dung' : 'Content type'}>{(['IELTS', 'TOEIC', 'interview'] as const).map(value => <button key={value} type="button" role="tab" aria-selected={section === value} onClick={() => { setSection(value); setShowImport(false); setDraft(null); setLoading(true); setError(''); }} className={`rounded-lg border-notebook px-3 py-2 text-sm ${section === value ? 'bg-stone-900 text-white' : ''}`}>{value === 'interview' ? (vi ? 'Phỏng vấn' : 'Interviews') : value}</button>)}</div>
      <div className="mt-4 flex flex-wrap gap-2"><button type="button" onClick={() => edit()} className="rounded-lg bg-amber-300 px-3 py-2 text-xs font-bold">+ {vi ? 'Tạo mới' : 'Create'}</button><button type="button" onClick={download} className="rounded-lg border-notebook px-3 py-2 text-xs font-bold"><DownloadOutlined /> {vi ? 'Tải JSON mẫu' : 'Download JSON example'}</button>
        <button type="button" onClick={() => { setShowImport(value => !value); setDraft(null); }} className="rounded-lg border-notebook px-3 py-2 text-xs font-bold"><UploadOutlined /> {vi ? 'Nhập JSON' : 'Import JSON'}</button></div>
      {showImport && <div className="mt-3 rounded-lg border-notebook p-3"><label className="cursor-pointer text-xs font-bold"><UploadOutlined /> {vi ? 'Chọn file JSON' : 'Choose JSON file'}<input type="file" accept=".json" onChange={async (event) => {
          const file = event.target.files?.[0]; event.target.value = ''; if (file) setValue(await file.text());
        }} /></label><textarea value={value} onChange={(event) => setValue(event.target.value)} rows={8} spellCheck={false} className="mt-3 w-full rounded-lg border-notebook p-3 font-mono text-xs" aria-label="Practice JSON" /><button type="button" onClick={() => void save()} disabled={saving} className="rounded-lg bg-stone-900 px-4 py-2 text-sm text-white">{vi ? 'Nhập nội dung' : 'Import content'}</button></div>}
      {draft && <form onSubmit={event => { event.preventDefault(); void saveItem(); }} className="mt-3 grid gap-3 rounded-lg border-notebook bg-stone-50 p-4">
        <label className="text-sm">{vi ? 'Tên đề / tình huống' : 'Title'}<input required value={String(draft.title ?? '')} onChange={event => setDraft({ ...draft, title: event.target.value })} className="mt-1 w-full rounded-lg border-notebook p-2" /></label>
        <label className="text-sm">{vi ? 'Trình độ' : 'Level'}<input required value={String(draft.level ?? '')} onChange={event => setDraft({ ...draft, level: event.target.value })} className="mt-1 w-full rounded-lg border-notebook p-2" /></label>
        {kind === 'certificate' ? <p className="text-sm font-bold">{vi ? 'Chứng chỉ' : 'Certificate'}: {section}</p> : <><label className="text-sm">{vi ? 'Nghề nghiệp' : 'Profession'}<input required value={String(draft.profession ?? '')} onChange={event => setDraft({ ...draft, profession: event.target.value })} className="mt-1 w-full rounded-lg border-notebook p-2" /></label><label className="text-sm">Rubric<textarea value={String(draft.rubric ?? '')} onChange={event => setDraft({ ...draft, rubric: event.target.value })} className="mt-1 w-full rounded-lg border-notebook p-2" /></label></>}
        {kind === 'certificate' && <label className="text-sm">{vi ? 'Thời gian làm bài (phút, để trống nếu không giới hạn)' : 'Time limit (minutes, blank for none)'}<input type="number" min={1} max={240} value={String(draft.durationMinutes ?? '')} onChange={event => { const value = event.target.value; const next = { ...draft }; if (value) next.durationMinutes = Number(value); else delete next.durationMinutes; setDraft(next); }} className="mt-1 w-full rounded-lg border-notebook p-2" /></label>}
        <SpecialPracticeQuestionEditor kind={kind} value={details} onChange={setDetails} vi={vi} />
        <div className="flex gap-2"><button type="submit" disabled={saving} className="rounded-lg bg-stone-900 px-4 py-2 text-sm text-white">{vi ? 'Lưu' : 'Save'}</button><button type="button" onClick={() => setDraft(null)} className="rounded-lg border-notebook px-4 py-2 text-sm">{vi ? 'Hủy' : 'Cancel'}</button></div></form>}
      {loading ? <p className="mt-4 text-sm">{vi ? 'Đang tải...' : 'Loading...'}</p> : <div className="mt-4 grid gap-2">{items.length === 0 ? <p className="text-sm text-stone-600">{vi ? 'Chưa có nội dung.' : 'No content yet.'}</p> : items.map(item => <div key={String(item.id)} className="flex items-center justify-between gap-3 rounded-lg border-notebook p-3"><div><strong>{String(item.title)}</strong><p className="text-xs text-stone-600">{String(item.certificate ?? item.profession ?? '')} · {String(item.level ?? '')}</p></div><div className="flex gap-2"><button type="button" onClick={() => edit(item)} className="rounded-lg border-notebook px-3 py-2 text-xs">{vi ? 'Sửa' : 'Edit'}</button><button type="button" disabled={saving} onClick={() => void removeItem(item)} className="rounded-lg border border-red-300 px-3 py-2 text-xs text-red-700">{vi ? 'Xóa' : 'Delete'}</button></div></div>)}</div>}
      {error && <p role="alert" className="mt-3 rounded-lg bg-rose-50 p-3 text-sm text-rose-800">{error}</p>}
    </section>
  </div>;
}
