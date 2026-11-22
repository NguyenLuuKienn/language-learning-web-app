import { useEffect, useState } from 'react';
import { Modal } from 'antd';
import { courseCategoriesApi, getErrorMessage, type CourseCategory, type CourseCategoryPayload } from '../../services/apiClient';
import { applyCourseCategories } from '../../utils/languageLevels';

type Group = 'domain' | 'cefr' | 'jlpt' | 'hsk' | 'topik';
const groups: Group[] = ['domain', 'cefr', 'jlpt', 'hsk', 'topik'];

export function CourseCategoryManager({ language, onClose, onChanged }: { language: 'vi' | 'en'; onClose: () => void; onChanged: () => void }) {
  const vi = language === 'vi';
  const [items, setItems] = useState<CourseCategory[]>([]);
  const [group, setGroup] = useState<Group>('domain');
  const [draft, setDraft] = useState<CourseCategory | CourseCategoryPayload | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const refresh = async () => {
    const next = await courseCategoriesApi.list();
    setItems(next); applyCourseCategories(next); onChanged();
  };
  useEffect(() => { void refresh().catch(cause => setError(getErrorMessage(cause, vi ? 'Không tải được danh mục.' : 'Could not load categories.'))); }, []);

  const startCreate = () => {
    setDraft({ kind: group === 'domain' ? 'domain' : 'level', framework: group === 'domain' ? '' : group,
      code: '', titleVi: '', titleEn: '', descriptionVi: '', descriptionEn: '', sortOrder: items.length });
    setError('');
  };
  const save = async () => {
    if (!draft) return;
    setBusy(true); setError('');
    try {
      const payload: CourseCategoryPayload = { kind: draft.kind, framework: draft.framework, code: draft.code,
        titleVi: draft.titleVi, titleEn: draft.titleEn, descriptionVi: draft.descriptionVi,
        descriptionEn: draft.descriptionEn, sortOrder: draft.sortOrder };
      if ('id' in draft) await courseCategoriesApi.update(draft.id, payload);
      else await courseCategoriesApi.create(payload);
      setDraft(null); await refresh();
    } catch (cause) { setError(getErrorMessage(cause, vi ? 'Không lưu được danh mục.' : 'Could not save category.')); }
    finally { setBusy(false); }
  };
  const remove = (item: CourseCategory) => Modal.confirm({
    title: vi ? `Xóa ${item.code}?` : `Delete ${item.code}?`,
    content: vi ? 'Không thể xóa khi đã có khóa học sử dụng.' : 'Categories used by courses cannot be deleted.',
    okType: 'danger', okText: vi ? 'Xóa' : 'Delete',
    onOk: async () => { try { await courseCategoriesApi.delete(item.id); await refresh(); }
      catch (cause) { setError(getErrorMessage(cause, vi ? 'Không xóa được danh mục.' : 'Could not delete category.')); } }
  });

  const visible = items.filter(item => group === 'domain' ? item.kind === 'domain' : item.kind === 'level' && item.framework === group);
  return <div className="fixed inset-0 z-[70] flex items-center justify-center bg-black/50 p-4" role="dialog" aria-modal="true" aria-label={vi ? 'Quản lý trình độ và lĩnh vực' : 'Manage levels and domains'}>
    <section className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-2xl border-notebook-2 bg-white p-5">
      <div className="flex items-center justify-between gap-3"><h2 className="text-lg font-bold">{vi ? 'Trình độ & lĩnh vực' : 'Levels & domains'}</h2><button type="button" onClick={onClose} className="rounded-lg border-notebook px-3 py-2 text-sm">{vi ? 'Đóng' : 'Close'}</button></div>
      <div className="mt-4 flex flex-wrap gap-2" role="tablist" aria-label={vi ? 'Nhóm danh mục' : 'Category group'}>{groups.map(value => <button key={value} type="button" role="tab" aria-selected={group === value} onClick={() => { setGroup(value); setDraft(null); }} className={`rounded-lg border-notebook px-3 py-2 text-xs font-bold ${group === value ? 'bg-stone-900 text-white' : ''}`}>{value === 'domain' ? (vi ? 'Lĩnh vực' : 'Domains') : value.toUpperCase()}</button>)}</div>
      <div className="mt-4 flex justify-end"><button type="button" onClick={startCreate} className="rounded-lg bg-amber-300 px-3 py-2 text-xs font-bold">+ {vi ? 'Thêm mới' : 'Add new'}</button></div>
      {draft && <form className="mt-3 grid gap-3 rounded-lg border-notebook bg-stone-50 p-4 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); void save(); }}>
        <label className="text-xs font-bold">{vi ? 'Mã' : 'Code'}<input required maxLength={50} disabled={'id' in draft} value={draft.code} onChange={event => setDraft({ ...draft, code: event.target.value })} className="mt-1 w-full rounded-lg border-notebook bg-white p-2" /></label>
        <label className="text-xs font-bold">{vi ? 'Thứ tự' : 'Order'}<input type="number" value={draft.sortOrder} onChange={event => setDraft({ ...draft, sortOrder: Number(event.target.value) })} className="mt-1 w-full rounded-lg border-notebook bg-white p-2" /></label>
        <label className="text-xs font-bold">{vi ? 'Tên tiếng Việt' : 'Vietnamese title'}<input required maxLength={150} value={draft.titleVi} onChange={event => setDraft({ ...draft, titleVi: event.target.value })} className="mt-1 w-full rounded-lg border-notebook bg-white p-2" /></label>
        <label className="text-xs font-bold">{vi ? 'Tên tiếng Anh' : 'English title'}<input required maxLength={150} value={draft.titleEn} onChange={event => setDraft({ ...draft, titleEn: event.target.value })} className="mt-1 w-full rounded-lg border-notebook bg-white p-2" /></label>
        <label className="text-xs font-bold">{vi ? 'Mô tả tiếng Việt' : 'Vietnamese description'}<input maxLength={500} value={draft.descriptionVi} onChange={event => setDraft({ ...draft, descriptionVi: event.target.value })} className="mt-1 w-full rounded-lg border-notebook bg-white p-2" /></label>
        <label className="text-xs font-bold">{vi ? 'Mô tả tiếng Anh' : 'English description'}<input maxLength={500} value={draft.descriptionEn} onChange={event => setDraft({ ...draft, descriptionEn: event.target.value })} className="mt-1 w-full rounded-lg border-notebook bg-white p-2" /></label>
        <div className="flex gap-2 sm:col-span-2"><button type="submit" disabled={busy} className="rounded-lg bg-stone-900 px-4 py-2 text-xs font-bold text-white">{vi ? 'Lưu' : 'Save'}</button><button type="button" onClick={() => setDraft(null)} className="rounded-lg border-notebook px-4 py-2 text-xs">{vi ? 'Hủy' : 'Cancel'}</button></div>
      </form>}
      <div className="mt-4 grid gap-2">{visible.length === 0 && <p className="text-sm text-stone-600">{vi ? 'Chưa có mục nào.' : 'No categories yet.'}</p>}{visible.map(item => <div key={item.id} className="flex items-center justify-between gap-3 rounded-lg border-notebook p-3"><div className="min-w-0"><strong>{item.code} · {vi ? item.titleVi : item.titleEn}</strong><p className="truncate text-xs text-stone-600">{vi ? item.descriptionVi : item.descriptionEn}</p></div><div className="flex gap-2"><button type="button" onClick={() => setDraft(item)} className="rounded-lg border-notebook px-2 py-1 text-xs">{vi ? 'Sửa' : 'Edit'}</button><button type="button" onClick={() => remove(item)} className="rounded-lg border border-rose-300 px-2 py-1 text-xs text-rose-700">{vi ? 'Xóa' : 'Delete'}</button></div></div>)}</div>
      {error && <p role="alert" className="mt-3 rounded-lg bg-rose-50 p-3 text-sm text-rose-800">{error}</p>}
    </section>
  </div>;
}
