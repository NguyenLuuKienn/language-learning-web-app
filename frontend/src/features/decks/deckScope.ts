import type { Deck, StudyTarget } from '../../types';

export function deckMatchesTarget(deck: Pick<Deck, 'languageId' | 'tags' | 'title'>, target: StudyTarget | null | undefined): boolean {
  if (!target) return false;
  if (deck.languageId !== target.languageId) return false;

  const taggedLevel = deck.tags.find((tag) => /^course-level:/i.test(tag))?.split(':')[1];
  const titledLevel = /^\s*(A1|A2|B1|B2|C1|C2|N[1-5]|HSK\s*[1-6]|TOPIK\s*[1-6])\b/i.exec(deck.title)?.[1];
  const deckLevel = taggedLevel || titledLevel;
  return !deckLevel || deckLevel.replace(/\s+/g, '').toLowerCase() === target.level.replace(/\s+/g, '').toLowerCase();
}
