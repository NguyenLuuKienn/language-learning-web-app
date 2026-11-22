// src/utils/sorting.ts
// Intelligent Natural Sorting Utilities for LinguaFlow

/**
 * Extracts the first numeric value from a string (e.g., "Unit 14: Giờ giấc..." -> 14, "Bài 02" -> 2)
 */
export const extractNumber = (text?: string): number | null => {
  if (!text) return null;
  // Match patterns like "Unit 01", "Unit 14", "Bài 2", "Lesson 03", or standalone numbers
  const match = text.match(/(?:unit|bài|lesson|chương|phần)\s*0*(\d+)/i) || text.match(/\b0*(\d+)\b/);
  if (match && match[1]) {
    const parsed = parseInt(match[1], 10);
    return isNaN(parsed) ? null : parsed;
  }
  return null;
};

/**
 * Natural sort for Units:
 * 1. Uses explicit sortOrder / order if > 0 and distinct
 * 2. Parses Unit number from title (e.g. Unit 01, Unit 02 ... Unit 14 ... Unit 45)
 * 3. Falls back to natural alphanumeric localeCompare
 */
export const sortUnits = <T extends { title?: string; sortOrder?: number; order?: number }>(units: T[]): T[] => {
  if (!units || !Array.isArray(units)) return [];
  return [...units].sort((a, b) => {
    const orderA = a.sortOrder ?? a.order ?? 0;
    const orderB = b.sortOrder ?? b.order ?? 0;

    // If both have explicit different non-zero orders
    if (orderA > 0 && orderB > 0 && orderA !== orderB) {
      return orderA - orderB;
    }

    // Try parsing numbers from titles
    const numA = extractNumber(a.title);
    const numB = extractNumber(b.title);

    if (numA !== null && numB !== null && numA !== numB) {
      return numA - numB;
    }

    // Fallback: natural Vietnamese / English alphanumeric compare
    return (a.title || '').localeCompare(b.title || '', undefined, { numeric: true, sensitivity: 'base' });
  });
};

/**
 * Natural sort for Lessons within a Unit:
 * Sorts Bài 1, Bài 2, Bài 3, Bài 4 ...
 */
export const sortLessons = <T extends { title?: string; sortOrder?: number; order?: number }>(lessons: T[]): T[] => {
  if (!lessons || !Array.isArray(lessons)) return [];
  return [...lessons].sort((a, b) => {
    const orderA = a.sortOrder ?? a.order ?? 0;
    const orderB = b.sortOrder ?? b.order ?? 0;

    if (orderA > 0 && orderB > 0 && orderA !== orderB) {
      return orderA - orderB;
    }

    const numA = extractNumber(a.title);
    const numB = extractNumber(b.title);

    if (numA !== null && numB !== null && numA !== numB) {
      return numA - numB;
    }

    return (a.title || '').localeCompare(b.title || '', undefined, { numeric: true, sensitivity: 'base' });
  });
};

/**
 * Sorts courses logically: by Language Code, then Level (A1 -> A2 -> B1 -> B2 -> C1 -> C2), then Title
 */
const LEVEL_WEIGHT: Record<string, number> = {
  a1: 1, n5: 1, 'hsk 1': 1, 'topik 1': 1,
  a2: 2, n4: 2, 'hsk 2': 2, 'topik 2': 2,
  b1: 3, n3: 3, 'hsk 3': 3, 'topik 3': 3,
  b2: 4, n2: 4, 'hsk 4': 4, 'topik 4': 4,
  c1: 5, n1: 5, 'hsk 5': 5, 'topik 5': 5,
  c2: 6, 'hsk 6': 6, 'topik 6': 6
};

export const sortCourses = <T extends { title?: string; level?: string; languageCode?: string; languageName?: string }>(courses: T[]): T[] => {
  if (!courses || !Array.isArray(courses)) return [];
  return [...courses].sort((a, b) => {
    const langA = (a.languageCode || a.languageName || '').toLowerCase();
    const langB = (b.languageCode || b.languageName || '').toLowerCase();
    if (langA !== langB) {
      return langA.localeCompare(langB);
    }

    const weightA = LEVEL_WEIGHT[(a.level || '').toLowerCase()] || 99;
    const weightB = LEVEL_WEIGHT[(b.level || '').toLowerCase()] || 99;
    if (weightA !== weightB) {
      return weightA - weightB;
    }

    return (a.title || '').localeCompare(b.title || '', undefined, { numeric: true });
  });
};

/**
 * Sorts languages: EN, JA, ZH, KO, FR, DE first, then alphabetically
 */
const LANG_PRIORITY: Record<string, number> = {
  en: 1,
  ja: 2,
  zh: 3,
  cn: 3,
  ko: 4,
  fr: 5,
  de: 6
};

export const sortLanguages = <T extends { code?: string; name?: string }>(languages: T[]): T[] => {
  if (!languages || !Array.isArray(languages)) return [];
  return [...languages].sort((a, b) => {
    const pA = LANG_PRIORITY[(a.code || '').toLowerCase()] || 99;
    const pB = LANG_PRIORITY[(b.code || '').toLowerCase()] || 99;
    if (pA !== pB) {
      return pA - pB;
    }
    return (a.name || '').localeCompare(b.name || '');
  });
};
