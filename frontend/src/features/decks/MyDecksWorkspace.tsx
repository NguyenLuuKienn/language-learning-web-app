
import type { ComponentProps } from 'react';

import type { LanguageOption } from './api/languagesApi';
import { DeckFormModal } from './components/DeckFormModal';
import { useDeckEditor } from './hooks/useDeckEditor';
import { useDeleteDeck } from './hooks/useDecks';
import { MyDecksPage } from './MyDecksPage';

import type { DeckResponse } from './types/deck.types';

type MyDecksWorkspaceProps = Omit<
  ComponentProps<typeof MyDecksPage>,
  'onCreateDeck' | 'onEditDeck' | 'onDeleteDeck'
> & {
  languages: LanguageOption[];
};

export function MyDecksWorkspace({
  languages,
  ...pageProps
}: MyDecksWorkspaceProps) {
  const editor = useDeckEditor();
  const deleteDeck = useDeleteDeck();

  const handleDeleteDeck = async (deck: DeckResponse) => {
    if (deleteDeck.isPending) return;

    const confirmed = window.confirm(
      `Bạn có chắc chắn muốn xóa bộ thẻ "${deck.title}" không?`
    );

    if (!confirmed) return;

    try {
      await deleteDeck.mutateAsync(deck.id);
    } catch {
      window.alert('Không thể xóa bộ thẻ. Vui lòng thử lại.');
    }
  };

  return (
    <>
      <MyDecksPage
        {...pageProps}
        onCreateDeck={editor.openCreate}
        onEditDeck={editor.openEdit}
        onDeleteDeck={handleDeleteDeck}
      />

      {editor.isOpen && (
        <DeckFormModal
          key={editor.editingDeck?.id ?? 'create'}
          deck={editor.editingDeck}
          languages={languages}
          onClose={editor.closeEditor}
          onSubmit={editor.submitDeck}
        />
      )}
    </>
  );
}
