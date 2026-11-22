import React, { useEffect, useState } from 'react';
import { App } from 'antd';
import { BookOutlined, CloseOutlined, DeleteOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';
import { getErrorMessage, notebookApi, type NotebookEntry, type NotebookEntryInput } from '../../services/apiClient';

interface NotebookWidgetProps {
  isAuthenticated: boolean;
  onRequestLogin: () => void;
  languageCode?: string;
  sourcePath: string;
  onDictionary?: (word: string) => void;
}

const emptyDraft = (languageCode = '', sourcePath = ''): NotebookEntryInput => ({
  text: '', note: '', languageCode, sourcePath, context: ''
});

let pendingDraftDuringLogin: NotebookEntryInput | null = null;

export const NotebookWidget: React.FC<NotebookWidgetProps> = ({ isAuthenticated, onRequestLogin, languageCode, sourcePath, onDictionary }) => {
  const { language } = useTranslation();
  const { message } = App.useApp();
  const vi = language === 'vi';
  const [open, setOpen] = useState(false);
  const [selection, setSelection] = useState<{ text: string; x: number; y: number } | null>(null);
  const [draft, setDraft] = useState<NotebookEntryInput>(() => {
    const pending = pendingDraftDuringLogin;
    pendingDraftDuringLogin = null;
    return pending ?? emptyDraft(languageCode, sourcePath);
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [items, setItems] = useState<NotebookEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const capture = () => {
      const active = document.activeElement;
      if (active instanceof HTMLInputElement || active instanceof HTMLTextAreaElement || active?.getAttribute('contenteditable') === 'true') {
        setSelection(null);
        return;
      }
      const selected = window.getSelection();
      const text = selected?.toString().trim() || '';
      if (!selected || selected.rangeCount === 0 || text.length < 1 || text.length > 500) {
        setSelection(null);
        return;
      }
      const rect = selected.getRangeAt(0).getBoundingClientRect();
      if (!rect.width || !rect.height) return;
      setSelection({
        text,
        x: Math.min(Math.max(rect.left, 12), window.innerWidth - 280),
        y: Math.min(rect.bottom + 8, window.innerHeight - 48)
      });
    };
    document.addEventListener('mouseup', capture);
    document.addEventListener('keyup', capture);
    return () => {
      document.removeEventListener('mouseup', capture);
      document.removeEventListener('keyup', capture);
    };
  }, []);

  useEffect(() => {
    if (!open || !isAuthenticated) return;
    let current = true;
    setLoading(true);
    notebookApi.list(debouncedSearch, page)
      .then((result) => { if (current) { setItems(result.items); setTotal(result.total); } })
      .catch((error) => { if (current) message.error(getErrorMessage(error, vi ? 'Không tải được sổ tay.' : 'Could not load notebook.')); })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, [open, isAuthenticated, debouncedSearch, page, message, vi]);

  const startSelected = () => {
    if (!selection) return;
    setDraft({ ...emptyDraft(languageCode, sourcePath), text: selection.text });
    setEditingId(null);
    setSelection(null);
    window.getSelection()?.removeAllRanges();
    setOpen(true);
  };

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!draft.text.trim() && !draft.note.trim()) return;
    if (!isAuthenticated) {
      pendingDraftDuringLogin = draft;
      onRequestLogin();
      return;
    }
    setSaving(true);
    try {
      if (editingId) await notebookApi.update(editingId, draft);
      else await notebookApi.create(draft);
      const result = await notebookApi.list(search, 1);
      setItems(result.items);
      setTotal(result.total);
      setPage(1);
      setEditingId(null);
      setDraft(emptyDraft(languageCode, sourcePath));
      message.success(vi ? 'Đã lưu vào sổ tay.' : 'Saved to notebook.');
    } catch (error) {
      message.error(getErrorMessage(error, vi ? 'Không lưu được ghi chú.' : 'Could not save note.'));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    try {
      await notebookApi.remove(id);
      setItems((current) => current.filter((entry) => entry.id !== id));
      setTotal((current) => Math.max(0, current - 1));
      if (editingId === id) { setEditingId(null); setDraft(emptyDraft(languageCode, sourcePath)); }
    } catch (error) {
      message.error(getErrorMessage(error, vi ? 'Không xóa được ghi chú.' : 'Could not delete note.'));
    }
  };

  return <>
    {selection && !open && <div style={{ left: selection.x, top: selection.y }} className="fixed z-[75] flex gap-1 rounded-lg border-notebook bg-white p-1 text-xs font-bold shadow-lg">
      {onDictionary && selection.text.length < 35 && <button type="button" onMouseDown={(event) => event.preventDefault()} onClick={() => { onDictionary(selection.text); setSelection(null); }} className="rounded px-2 py-1 hover:bg-amber-50"><BookOutlined /> {vi ? 'Từ điển' : 'Dictionary'}</button>}
      <button type="button" onMouseDown={(event) => event.preventDefault()} onClick={startSelected} className="rounded px-2 py-1 hover:bg-amber-50"><PlusOutlined /> {vi ? 'Thêm vào Sổ tay' : 'Add to notebook'}</button>
    </div>}
    <button
      type="button"
      onClick={() => { setSelection(null); setOpen(true); }}
      aria-label={vi ? 'Mở Sổ tay' : 'Open notebook'}
      className="fixed right-3 top-1/2 z-[70] -translate-y-1/2 rounded-l-xl border-notebook-2 bg-amber-300 px-3 py-4 text-stone-900 shadow-md hover:bg-amber-200"
    ><BookOutlined className="text-lg" /></button>
    {open && <div className="fixed inset-0 z-[80] bg-black/40" onMouseDown={(event) => { if (event.target === event.currentTarget) setOpen(false); }}>
      <section role="dialog" aria-modal="true" aria-label={vi ? 'Sổ tay' : 'Notebook'} className="absolute inset-y-0 right-0 flex w-full max-w-md flex-col border-l-2 border-stone-900 bg-[#FAF6EE] shadow-xl">
        <header className="flex items-center justify-between border-b-2 border-stone-900 p-4">
          <h2 className="text-lg font-bold"><BookOutlined /> {vi ? 'Sổ tay' : 'Notebook'}</h2>
          <button type="button" aria-label={vi ? 'Đóng' : 'Close'} onClick={() => setOpen(false)}><CloseOutlined /></button>
        </header>
        <div className="flex-1 space-y-4 overflow-y-auto p-4">
          <form onSubmit={save} className="space-y-2 rounded-xl border-notebook bg-white p-3">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-bold">{editingId ? (vi ? 'Sửa ghi chú' : 'Edit note') : (vi ? 'Ghi chú mới' : 'New note')}</h3>
              {editingId && <button type="button" onClick={() => { setEditingId(null); setDraft(emptyDraft(languageCode, sourcePath)); }} className="text-xs underline">{vi ? 'Tạo mới' : 'New'}</button>}
            </div>
            <input maxLength={500} value={draft.text} onChange={(event) => setDraft({ ...draft, text: event.target.value })} placeholder={vi ? 'Từ hoặc cụm từ (tùy chọn)' : 'Word or phrase (optional)'} className="w-full rounded-lg border border-stone-300 px-3 py-2 text-sm" />
            <textarea maxLength={5000} rows={4} value={draft.note} onChange={(event) => setDraft({ ...draft, note: event.target.value })} placeholder={vi ? 'Viết ghi chú của bạn...' : 'Write your note...'} className="w-full rounded-lg border border-stone-300 px-3 py-2 text-sm" />
            {!isAuthenticated && <p className="text-xs text-stone-600">{vi ? 'Đăng nhập để lưu và đồng bộ ghi chú.' : 'Sign in to save and sync notes.'}</p>}
            <button disabled={saving || (!draft.text.trim() && !draft.note.trim())} className="rounded-lg bg-stone-900 px-3 py-2 text-xs font-bold text-white disabled:opacity-50"><SaveOutlined /> {vi ? 'Lưu' : 'Save'}</button>
          </form>
          {isAuthenticated && <>
            <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder={vi ? 'Tìm trong sổ tay...' : 'Search notes...'} className="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm" />
            {loading && <p className="text-sm">{vi ? 'Đang tải...' : 'Loading...'}</p>}
            {!loading && items.length === 0 && <p className="rounded-xl border-notebook bg-white p-4 text-sm text-stone-600">{vi ? 'Chưa có ghi chú.' : 'No notes yet.'}</p>}
            {items.map((entry) => <article key={entry.id} className="rounded-xl border-notebook bg-white p-3">
              {entry.text && <h3 className="break-words font-bold">{entry.text}</h3>}
              {entry.note && <p className="mt-1 whitespace-pre-wrap break-words text-sm">{entry.note}</p>}
              <div className="mt-2 flex items-center justify-between text-xs text-stone-500">
                <span>{new Date(entry.createdAt).toLocaleDateString(language)}</span>
                <div className="flex gap-3">
                  <button type="button" onClick={() => { setEditingId(entry.id); setDraft({ text: entry.text, note: entry.note, languageCode: entry.languageCode, sourcePath: entry.sourcePath, context: entry.context }); }} className="underline">{vi ? 'Sửa' : 'Edit'}</button>
                  <button type="button" onClick={() => void remove(entry.id)} aria-label={vi ? 'Xóa' : 'Delete'} className="text-rose-600"><DeleteOutlined /></button>
                </div>
              </div>
            </article>)}
            {total > 20 && <div className="flex items-center justify-center gap-3 text-xs">
              <button disabled={page <= 1} onClick={() => setPage(page - 1)}>{vi ? 'Trước' : 'Previous'}</button>
              <span>{page} / {Math.ceil(total / 20)}</span>
              <button disabled={page * 20 >= total} onClick={() => setPage(page + 1)}>{vi ? 'Sau' : 'Next'}</button>
            </div>}
          </>}
        </div>
      </section>
    </div>}
  </>;
};
