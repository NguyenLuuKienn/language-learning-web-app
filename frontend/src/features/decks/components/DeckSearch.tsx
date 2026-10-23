
import { Search, X } from 'lucide-react';

interface DeckSearchProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export function DeckSearch({
  value,
  onChange,
  placeholder = 'Tìm kiếm bộ thẻ...',
}: DeckSearchProps) {
  return (
    <div className="relative w-full">
      <Search
        size={19}
        aria-hidden="true"
        className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-stone-400"
      />

      <input
        type="text"
        inputMode="search"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        aria-label="Tìm kiếm bộ thẻ"
        className="w-full rounded-xl border-2 border-stone-800 bg-white py-3 pl-12 pr-12 text-sm text-stone-900 outline-none transition focus:border-rose-600 focus:ring-2 focus:ring-rose-100"
      />

      {value.length > 0 && (
        <button
          type="button"
          onClick={() => onChange('')}
          aria-label="Xóa nội dung tìm kiếm"
          className="absolute right-3 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-stone-800"
        >
          <X size={18} />
        </button>
      )}
    </div>
  );
}
