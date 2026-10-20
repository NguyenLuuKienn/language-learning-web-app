
import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { LoaderCircle, X } from 'lucide-react';

import type {
  CreateDeckRequest,
  DeckResponse,
} from '../types/deck.types';

export interface LanguageOption {
  id: string;
  name: string;
}

interface DeckFormModalProps {
  deck?: DeckResponse | null;
  languages: LanguageOption[];
  onClose: () => void;
  onSubmit: (request: CreateDeckRequest) => Promise<void>;
}

export function DeckFormModal({
  deck,
  languages,
  onClose,
  onSubmit,
}: DeckFormModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const isEditing = Boolean(deck);

  const [title, setTitle] = useState(deck?.title ?? '');
  const [description, setDescription] = useState(
    deck?.description ?? ''
  );
  const [languageId, setLanguageId] = useState(
    deck?.languageId ?? ''
  );
  const [isPublic, setIsPublic] = useState(
    deck?.isPublic ?? false
  );
  const [tags, setTags] = useState(
    deck?.tags.join(', ') ?? ''
  );

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');


useEffect(() => {
  const dialog = dialogRef.current;

  if (!dialog) return;

  if (!dialog.open) {
    dialog.showModal();
  }

  return () => {
    if (dialog.open) {
      dialog.close();
    }
  };
}, []);

  const hasValidLanguage = languages.some(
    (language) => language.id === languageId
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    const normalizedTitle = title.trim();

    if (!normalizedTitle) {
      setError('Vui lòng nhập tên bộ thẻ.');
      return;
    }

    if (normalizedTitle.length > 200) {
      setError('Tên bộ thẻ không được vượt quá 200 ký tự.');
      return;
    }

    if (!hasValidLanguage) {
      setError('Vui lòng chọn ngôn ngữ hợp lệ.');
      return;
    }

    const normalizedTags = [
      ...new Set(
        tags
          .split(',')
          .map((tag) => tag.trim())
          .filter(Boolean)
      ),
    ];

    const request: CreateDeckRequest = {
      title: normalizedTitle,
      description: description.trim(),
      languageId,
      isPublic,
      tags: normalizedTags,
    };

    try {
      setIsSubmitting(true);
      await onSubmit(request);
      onClose();
    } catch {
      setError('Không thể lưu bộ thẻ. Vui lòng thử lại.');
    } finally {
      setIsSubmitting(false);
    }
  }


return (
  <dialog
    ref={dialogRef}
    onCancel={(event) => {
      event.preventDefault();

      if (!isSubmitting) {
        onClose();
      }
    }}
    aria-labelledby="deck-modal-title"
    className="w-[calc(100%-2rem)] max-w-xl rounded-2xl border-2 border-stone-900 bg-[#FAF6EE] p-0 text-stone-900 shadow-xl backdrop:bg-stone-900/60"
  >
      <div className="flex items-center justify-between border-b-2 border-stone-900 px-6 py-5">
        <div>
          <h2
            id="deck-modal-title"
            className="text-xl font-extrabold"
          >
            {isEditing ? 'Chỉnh sửa bộ thẻ' : 'Tạo bộ thẻ mới'}
          </h2>

          <p className="mt-1 text-sm text-stone-500">
            {isEditing
              ? 'Cập nhật thông tin bộ thẻ của bạn.'
              : 'Thiết lập thông tin cho bộ thẻ học tập.'}
          </p>
        </div>

        <button
          type="button"
          onClick={onClose}
          disabled={isSubmitting}
          aria-label="Đóng cửa sổ"
          className="rounded-lg p-2 text-stone-500 hover:bg-stone-200 disabled:opacity-50"
        >
          <X size={21} />
        </button>
      </div>

      <form onSubmit={handleSubmit} className="p-6">
        <div className="space-y-5">
          <div>
            <label
              htmlFor="deck-title"
              className="mb-2 block text-sm font-bold"
            >
              Tên bộ thẻ *
            </label>

            <input
              id="deck-title"
              type="text"
              required
              autoFocus
              maxLength={200}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="Ví dụ: English Vocabulary A1"
              className="w-full rounded-lg border-2 border-stone-300 bg-white px-4 py-3 text-sm outline-none focus:border-rose-600"
            />
          </div>

          <div>
            <label
              htmlFor="deck-description"
              className="mb-2 block text-sm font-bold"
            >
              Mô tả
            </label>

            <textarea
              id="deck-description"
              rows={3}
              value={description}
              onChange={(event) =>
                setDescription(event.target.value)
              }
              placeholder="Mô tả nội dung bộ thẻ..."
              className="w-full resize-y rounded-lg border-2 border-stone-300 bg-white px-4 py-3 text-sm outline-none focus:border-rose-600"
            />
          </div>

          <div>
            <label
              htmlFor="deck-language"
              className="mb-2 block text-sm font-bold"
            >
              Ngôn ngữ *
            </label>

            <select
              id="deck-language"
              required
              value={languageId}
              onChange={(event) =>
                setLanguageId(event.target.value)
              }
              disabled={languages.length === 0}
              className="w-full rounded-lg border-2 border-stone-300 bg-white px-4 py-3 text-sm outline-none focus:border-rose-600 disabled:bg-stone-100"
            >
              <option value="">Chọn ngôn ngữ</option>

              {languages.map((language) => (
                <option key={language.id} value={language.id}>
                  {language.name}
                </option>
              ))}
            </select>

            {languages.length === 0 && (
              <p className="mt-2 text-xs text-amber-700">
                Danh sách ngôn ngữ chưa sẵn sàng.
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="deck-visibility"
              className="mb-2 block text-sm font-bold"
            >
              Quyền riêng tư
            </label>

            <select
              id="deck-visibility"
              value={isPublic ? 'public' : 'private'}
              onChange={(event) =>
                setIsPublic(event.target.value === 'public')
              }
              className="w-full rounded-lg border-2 border-stone-300 bg-white px-4 py-3 text-sm outline-none focus:border-rose-600"
            >
              <option value="private">
                Riêng tư — Chỉ mình tôi
              </option>
              <option value="public">
                Công khai — Người khác có thể xem
              </option>
            </select>
          </div>

          <div>
            <label
              htmlFor="deck-tags"
              className="mb-2 block text-sm font-bold"
            >
              Tags
            </label>

            <input
              id="deck-tags"
              type="text"
              value={tags}
              onChange={(event) => setTags(event.target.value)}
              placeholder="English, Grammar, Vocabulary"
              className="w-full rounded-lg border-2 border-stone-300 bg-white px-4 py-3 text-sm outline-none focus:border-rose-600"
            />

            <p className="mt-1 text-xs text-stone-500">
              Phân cách các tag bằng dấu phẩy.
            </p>
          </div>
        </div>

        {error && (
          <p
            role="alert"
            className="mt-5 rounded-lg bg-red-50 p-3 text-sm text-red-700"
          >
            {error}
          </p>
        )}

        <div className="mt-7 flex flex-wrap justify-end gap-3 border-t border-stone-200 pt-5">
          <button
            type="button"
            disabled={isSubmitting}
            onClick={onClose}
            className="rounded-lg border-2 border-stone-300 bg-white px-5 py-2.5 text-sm font-semibold hover:bg-stone-100 disabled:opacity-50"
          >
            Hủy
          </button>

          <button
            type="submit"
            disabled={isSubmitting || !hasValidLanguage}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-stone-900 px-5 py-2.5 text-sm font-semibold text-white hover:bg-stone-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isSubmitting && (
              <LoaderCircle size={16} className="animate-spin" />
            )}

            {isSubmitting
              ? 'Đang lưu...'
              : isEditing
                ? 'Lưu thay đổi'
                : 'Tạo bộ thẻ'}
          </button>
        </div>
      </form>
    </dialog>
  );
}
