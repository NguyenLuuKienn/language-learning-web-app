import Dexie, { type Table } from 'dexie';
import type { Deck, Flashcard, ReviewEvent, SyncOperation } from '../types';

export class AppIndexedDb extends Dexie {
  decks!: Table<Deck, string>;
  cards!: Table<Flashcard, string>;
  offlineReviews!: Table<ReviewEvent, string>;
  syncQueue!: Table<SyncOperation, string>;
  syncMetadata!: Table<{ userId: string; cursor?: string; lastSyncedAt?: string }, string>;

  constructor() {
    super('LanguageLearningOfflineDb');
    this.version(1).stores({
      decks: 'id, ownerId, languageId, visibility',
      cards: 'id, deckId',
      offlineReviews: 'clientEventId, cardId, reviewedAt',
      syncQueue: 'operationId, type, status, occurredAt'
    });
    this.version(2).stores({
      decks: 'id, ownerId, languageId, visibility',
      cards: 'id, deckId',
      offlineReviews: 'clientEventId, cardId, reviewedAt',
      syncQueue: 'operationId, type, status, occurredAt',
      syncMetadata: 'userId'
    });
  }
}

export const db = new AppIndexedDb();

export async function clearOfflineStudyData(): Promise<void> {
  await db.transaction('rw', db.decks, db.cards, db.offlineReviews, db.syncQueue, db.syncMetadata, async () => {
    await db.decks.clear();
    await db.cards.clear();
    await db.offlineReviews.clear();
    await db.syncQueue.clear();
    await db.syncMetadata.clear();
  });
}
