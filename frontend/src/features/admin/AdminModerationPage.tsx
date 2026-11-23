import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, Navigate } from 'react-router-dom';
import { useAuth } from '../auth/authContext';
import { moderationApi } from './api/moderationApi';

type View = 'users' | 'decks';

export function AdminModerationPage() {
  const { user } = useAuth();
  const cache = useQueryClient();
  const [view, setView] = useState<View>('users');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const users = useQuery({ queryKey: ['admin-users', page, search], queryFn: () => moderationApi.users(page, search), enabled: user?.role === 'Admin' && view === 'users' });
  const decks = useQuery({ queryKey: ['admin-decks', page, search], queryFn: () => moderationApi.decks(page, search), enabled: user?.role === 'Admin' && view === 'decks' });
  const action = useMutation({
    mutationFn: async (target: { kind: View; id: string; value: boolean }) =>
      target.kind === 'users' ? moderationApi.setUserActive(target.id, target.value) : moderationApi.setDeckHidden(target.id, target.value),
    onSuccess: () => { void cache.invalidateQueries({ queryKey: ['admin-users'] }); void cache.invalidateQueries({ queryKey: ['admin-decks'] }); },
  });

  if (user?.role !== 'Admin') return <Navigate to="/" replace />;
  const result = view === 'users' ? users : decks;
  const totalPages = result.data?.totalPages ?? 0;

  return (
    <main className="graph-paper min-h-screen p-6 text-stone-900">
      <div className="mx-auto max-w-5xl rounded-2xl border-2 border-stone-900 bg-white p-6 shadow-[5px_5px_0_#262626]">
        <Link to="/" className="text-sm font-semibold underline">Về trang chủ</Link>
        <h1 className="mt-4 text-2xl font-extrabold">Quản trị người dùng và bộ thẻ</h1>
        <div className="mt-5 flex gap-3" role="tablist" aria-label="Nội dung quản trị">
          <button type="button" role="tab" aria-selected={view === 'users'} onClick={() => { setView('users'); setPage(1); }} className="rounded-lg border-2 px-4 py-2 font-bold">Người dùng</button>
          <button type="button" role="tab" aria-selected={view === 'decks'} onClick={() => { setView('decks'); setPage(1); }} className="rounded-lg border-2 px-4 py-2 font-bold">Bộ thẻ công khai</button>
        </div>
        <form className="mt-5 flex gap-2" onSubmit={(event) => { event.preventDefault(); setPage(1); setSearch(searchInput); }}>
          <input aria-label="Tìm kiếm" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} className="min-w-0 flex-1 rounded-lg border-2 p-2" placeholder={view === 'users' ? 'Email hoặc tên' : 'Tên bộ thẻ'} />
          <button className="rounded-lg bg-stone-900 px-4 py-2 font-bold text-white">Tìm</button>
        </form>
        {result.isPending && <p role="status" className="mt-5">Đang tải...</p>}
        {result.isError && <p role="alert" className="mt-5 text-red-700">Không tải được danh sách.</p>}
        {action.isError && <p role="alert" className="mt-5 text-red-700">Không thực hiện được thao tác: {action.error.message}</p>}
        {result.data?.items.length === 0 && <p className="mt-5">Không có kết quả.</p>}
        <ul className="mt-5 space-y-3">
          {view === 'users' && users.data?.items.map((item) => <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4"><div><strong>{item.displayName}</strong><p>{item.email} · {item.role} · {item.isActive ? 'Đang hoạt động' : 'Đã khóa'}</p></div><button disabled={action.isPending || item.id === user.id} onClick={() => action.mutate({ kind: 'users', id: item.id, value: !item.isActive })} className="rounded-lg border-2 px-3 py-2 font-bold disabled:opacity-50">{item.isActive ? 'Khóa' : 'Mở khóa'}</button></li>)}
          {view === 'decks' && decks.data?.items.map((item) => <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4"><div><strong>{item.title}</strong><p>{item.isModerated ? 'Đã ẩn bởi quản trị viên' : 'Công khai'}</p></div><button disabled={action.isPending} onClick={() => action.mutate({ kind: 'decks', id: item.id, value: !item.isModerated })} className="rounded-lg border-2 px-3 py-2 font-bold disabled:opacity-50">{item.isModerated ? 'Bỏ ẩn' : 'Ẩn'}</button></li>)}
        </ul>
        {totalPages > 1 && <nav aria-label="Phân trang" className="mt-5 flex items-center gap-4"><button disabled={page === 1} onClick={() => setPage(page - 1)}>Trang trước</button><span>{page}/{totalPages}</span><button disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Trang sau</button></nav>}
      </div>
    </main>
  );
}
