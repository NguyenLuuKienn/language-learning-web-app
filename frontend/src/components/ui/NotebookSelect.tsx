import { useEffect, useRef, useState, type ReactNode } from 'react';
import { CheckOutlined, DownOutlined, SearchOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';

export interface SelectOption {
  value: string;
  label: string;
  icon?: ReactNode;
  badge?: string;
}

interface NotebookSelectProps {
  value: string;
  onChange: (value: string) => void;
  options: SelectOption[];
  placeholder?: string;
  label?: string;
  className?: string;
  disabled?: boolean;
  prefix?: ReactNode;
  searchable?: boolean;
}

export function NotebookSelect({
  value,
  onChange,
  options,
  placeholder,
  label,
  className = '',
  disabled = false,
  prefix,
  searchable = false
}: NotebookSelectProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const optionRefs = useRef<(HTMLButtonElement | null)[]>([]);
  const selected = options.find((option) => option.value === value);
  const matches = searchable
    ? options.filter((option) => option.label.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase()))
    : options;
  const visible = searchable ? matches.slice(0, 50) : matches;
  const accessibleLabel = label ?? placeholder ?? t('common.selectOption');

  useEffect(() => {
    if (!open) return;
    if (searchable) inputRef.current?.focus();
    const closeOnOutsideClick = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', closeOnOutsideClick);
    return () => document.removeEventListener('mousedown', closeOnOutsideClick);
  }, [open, searchable]);

  useEffect(() => {
    if (open && searchable) optionRefs.current[activeIndex]?.scrollIntoView({ block: 'nearest' });
  }, [activeIndex, open, searchable]);

  const choose = (option: SelectOption) => {
    onChange(option.value);
    setOpen(false);
    setQuery('');
    setActiveIndex(0);
    triggerRef.current?.focus();
  };

  return <div ref={containerRef} className={`relative inline-block min-w-0 max-w-full select-none ${className}`}>
    <button
      ref={triggerRef}
      type="button"
      disabled={disabled}
      aria-label={accessibleLabel}
      aria-haspopup="listbox"
      aria-expanded={open}
      title={selected?.label}
      onClick={() => setOpen((current) => !current)}
      className={`flex w-full min-w-0 items-center justify-between gap-2 overflow-hidden rounded-xl border-notebook bg-white px-3 py-2 text-left text-xs font-bold text-stone-900 shadow-2xs transition hover:border-stone-500 disabled:cursor-not-allowed disabled:opacity-50 ${open ? 'border-stone-800 ring-2 ring-amber-300/70' : ''}`}
    >
      <span className="flex min-w-0 items-center gap-1.5 overflow-hidden">
        {prefix && <span className="shrink-0">{prefix}</span>}
        {selected?.icon && <span className="shrink-0">{selected.icon}</span>}
        <span className="min-w-0 truncate">{selected?.label ?? placeholder ?? t('common.selectOption')}</span>
        {selected?.badge && <span className="shrink-0 rounded bg-amber-100 px-1.5 text-[10px] text-amber-800">{selected.badge}</span>}
      </span>
      <DownOutlined className={`shrink-0 text-[10px] text-stone-500 transition-transform ${open ? 'rotate-180' : ''}`} />
    </button>

    {open && <div className="absolute left-0 right-0 z-50 mt-1.5 rounded-xl border-2 border-[#262626] bg-[#FAF6EE] p-1 shadow-notebook">
      {searchable && <div className="relative mb-1">
        <SearchOutlined className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400" />
        <input
          ref={inputRef}
          value={query}
          onChange={(event) => { setQuery(event.target.value); setActiveIndex(0); }}
          onKeyDown={(event) => {
            if (event.key === 'Escape') { setOpen(false); triggerRef.current?.focus(); }
            if (event.key === 'ArrowDown') { event.preventDefault(); setActiveIndex((index) => Math.min(index + 1, Math.max(visible.length - 1, 0))); }
            if (event.key === 'ArrowUp') { event.preventDefault(); setActiveIndex((index) => Math.max(index - 1, 0)); }
            if (event.key === 'Enter' && visible[activeIndex]) { event.preventDefault(); choose(visible[activeIndex]); }
          }}
          aria-label={`${t('common.searchItems')} ${accessibleLabel}`}
          placeholder={t('common.searchItems')}
          className="w-full rounded-lg border border-stone-300 bg-white py-2 pl-9 pr-3 text-sm"
        />
      </div>}
      <div role="listbox" aria-label={accessibleLabel} className="max-h-60 overflow-y-auto notebook-scrollbar">
        {visible.map((option, index) => <button
          ref={(element) => { optionRefs.current[index] = element; }}
          key={option.value}
          type="button"
          role="option"
          aria-selected={option.value === value}
          onMouseEnter={() => setActiveIndex(index)}
          onClick={() => choose(option)}
          className={`flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-left text-xs transition ${index === activeIndex ? 'bg-amber-100/80' : 'hover:bg-white'}`}
        >
          <span className="flex min-w-0 items-center gap-2 overflow-hidden">
            {option.icon && <span className="shrink-0">{option.icon}</span>}
            <span className="min-w-0 truncate" title={option.label}>{option.label}</span>
            {option.badge && <span className="shrink-0 rounded bg-stone-200 px-1.5 text-[9px]">{option.badge}</span>}
          </span>
          {option.value === value && <CheckOutlined className="shrink-0" />}
        </button>)}
        {visible.length === 0 && <p className="p-3 text-center text-xs text-stone-500">{t('common.none')}</p>}
      </div>
      {matches.length > visible.length && <p className="px-2 pt-2 text-xs text-stone-500">{t('common.searchToNarrow', { count: matches.length })}</p>}
    </div>}
  </div>;
}
