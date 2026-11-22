import { useTranslation } from '../../i18n';

interface PaginationProps {
  page: number;
  pageCount: number;
  onChange: (page: number) => void;
}

export function Pagination({ page, pageCount, onChange }: PaginationProps) {
  const { t } = useTranslation();
  if (pageCount <= 1) return null;

  return <nav aria-label={t('common.pagination')} className="flex items-center justify-center gap-3 text-sm">
    <button type="button" disabled={page <= 1} onClick={() => onChange(page - 1)} className="rounded-lg border-notebook bg-white px-3 py-2 disabled:opacity-40">{t('common.previous')}</button>
    <span aria-live="polite">{t('common.pageOf', { page, total: pageCount })}</span>
    <button type="button" disabled={page >= pageCount} onClick={() => onChange(page + 1)} className="rounded-lg border-notebook bg-white px-3 py-2 disabled:opacity-40">{t('common.next')}</button>
  </nav>;
}
