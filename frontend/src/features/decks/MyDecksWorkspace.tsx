
import type { ComponentProps } from 'react';

import { DeckFormModal } from './components/DeckFormModal';
import { useDeckEditor } from './hooks/useDeckEditor';
import { useLanguages } from './hooks/useLanguages';
import { MyDecksPage } from './MyDecksPage';

type MyDecksWorkspaceProps = Omit<
  ComponentProps<typeof MyDecksPage>,
  'onCreateDeck' | 'onEditDeck'
>;

export function MyDecksWorkspace(
  pageProps: MyDecksWorkspaceProps
) {
  const editor = useDeckEditor();

  const { data: languages = [] } = useLanguages();

  return (
    <>
      <MyDecksPage
        {...pageProps}
        onCreateDeck={editor.openCreate}
        onEditDeck={editor.openEdit}
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
