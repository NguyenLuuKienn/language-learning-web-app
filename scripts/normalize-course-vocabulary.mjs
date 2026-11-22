import { readFileSync, writeFileSync } from 'node:fs';

const path = process.argv[2];
if (!path) {
  process.stderr.write('Usage: node scripts/normalize-course-vocabulary.mjs <course.json>\n');
  process.exit(1);
}

const material = JSON.parse(readFileSync(path, 'utf8'));
let count = 0;
const vocabularyByFace = new Map();
const vocabularyByTerm = new Map();
for (const unit of material.units ?? []) {
  for (const lesson of unit.lessons ?? []) {
    for (const card of lesson.flashcards ?? []) {
      if (!Array.isArray(card.meanings) || card.meanings.length === 0) {
        card.meanings = [card.back];
        count++;
      }
      vocabularyByFace.set(`${card.front.trim().toLocaleLowerCase()}\u0000${card.back.trim().toLocaleLowerCase()}`, card);
      const termKey = card.front.trim().toLocaleLowerCase();
      const matches = vocabularyByTerm.get(termKey) ?? [];
      matches.push(card);
      vocabularyByTerm.set(termKey, matches);
    }
  }
}
let deckCount = 0;
for (const deck of material.decks ?? []) {
  for (const card of deck.cards ?? []) {
    const termKey = card.front.trim().toLocaleLowerCase();
    const source = vocabularyByFace.get(`${termKey}\u0000${card.back.trim().toLocaleLowerCase()}`)
      ?? (vocabularyByTerm.get(termKey)?.length === 1 ? vocabularyByTerm.get(termKey)[0] : undefined);
    if (!source) continue;
    for (const property of ['phonetic', 'partOfSpeech', 'meanings', 'example']) {
      if (source[property] !== undefined && card[property] === undefined) card[property] = source[property];
    }
    deckCount++;
  }
}
writeFileSync(path, JSON.stringify(material, null, 2) + '\n', 'utf8');
process.stdout.write(`Normalized ${count} flashcards and linked ${deckCount} deck cards. Existing translations remain the only verified meanings.\n`);
