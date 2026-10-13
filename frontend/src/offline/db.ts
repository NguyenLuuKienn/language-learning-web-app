import Dexie, { type Table } from 'dexie';
import type { Deck, Flashcard, ReviewEvent, SyncOperation } from '../types';

export class AppIndexedDb extends Dexie {
  decks!: Table<Deck, string>;
  cards!: Table<Flashcard, string>;
  offlineReviews!: Table<ReviewEvent, string>;
  syncQueue!: Table<SyncOperation, string>;

  constructor() {
    super('LanguageLearningOfflineDb');
    this.version(1).stores({
      decks: 'id, ownerId, languageId, visibility',
      cards: 'id, deckId',
      offlineReviews: 'clientEventId, cardId, reviewedAt',
      syncQueue: 'operationId, type, status, occurredAt'
    });
  }
}

export const db = new AppIndexedDb();
