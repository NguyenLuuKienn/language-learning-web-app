
import { db } from './db';

import type {
  ReviewEvent,
  SyncOperation,
  SrsRating,
} from '../types';

export async function queueOfflineReview(
  deckId: string,
  cardId: string,
  rating: SrsRating
): Promise<ReviewEvent> {
  if (!deckId || !cardId) {
    throw new Error('Deck ID and Card ID are required.');
  }

  const validRatings: SrsRating[] = [
    'Again',
    'Hard',
    'Good',
    'Easy',
  ];

  if (!validRatings.includes(rating)) {
    throw new Error('Invalid review rating.');
  }

  const clientEventId = crypto.randomUUID();
  const operationId = crypto.randomUUID();
  const reviewedAt = new Date().toISOString();

  return db.transaction(
    'rw',
    db.decks,
    db.cards,
    db.cardSchedules,
    db.offlineReviews,
    db.syncQueue,
    async () => {
      const deck = await db.decks.get(deckId);
      const card = await db.cards.get(cardId);
      const schedule = await db.cardSchedules.get(cardId);

      if (
        !deck ||
        !card ||
        !schedule ||
        card.deckId !== deckId ||
        schedule.deckId !== deckId
      ) {
        throw new Error(
          'Deck or card is not available offline.'
        );
      }

      const reviewEvent: ReviewEvent = {
        clientEventId,
        cardId,
        rating,
        reviewedAt,
        algorithmVersion: schedule.algorithmVersion,
      };

      const syncOperation: SyncOperation = {
        operationId,
        type: 'Review',
        entityId: cardId,
        payload: {
          clientEventId,
          cardId,
          rating,
          reviewedAt,
          algorithmVersion: schedule.algorithmVersion,
        },
        occurredAt: reviewedAt,
        status: 'Pending',
      };

      await db.offlineReviews.add(reviewEvent);
      await db.syncQueue.add(syncOperation);

      return reviewEvent;
    }
  );
}
