import { sortUnits, sortLessons } from '../utils/sorting';

export interface QuizQuestionItem {
  id: string;
  question: string;
  options: string[];
  correctIndex: number;
  explanation: string;
  type?: 'multiple_choice' | 'fill_in_blank';
}

export interface FlashcardItem {
  id: string;
  front: string;
  back: string;
  meanings?: string[];
  partOfSpeech?: string;
  phonetic?: string;
  example?: string;
  exampleTranslation?: string;
  clozeSentence?: string;
  acceptedAnswers?: string[];
  audioUrl?: string;
  imageUrl?: string;
  tags?: string[];
}

export interface ListeningItem {
  id: string;
  title: string;
  audioScript: string;
  speed: number;
  question: string;
  options: string[];
  correctIndex: number;
  explanation?: string;
}

export interface SpeakingItem {
  id: string;
  title: string;
  phrase: string;
  phonetic?: string;
  translation: string;
  context?: string;
}

export interface GrammarItem {
  id: string;
  title: string;
  formula: string;
  explanation: string;
  example: string;
  examples?: string[];
  exampleTranslation: string;
  question: string;
  options: string[];
  correctIndex: number;
}

export interface ReadingVocabularyItem {
  word: string;
  ipa?: string;
  meaning: string;
}

export interface ReadingItem {
  id: string;
  title: string;
  category?: string;
  readTime?: string;
  content: string[];
  vocabularies: ReadingVocabularyItem[];
  quiz: {
    question: string;
    options: string[];
    correctIndex: number;
    explanation?: string;
  };
}

export interface WritingItem {
  id: string;
  title: string;
  scenario: string;
  instruction: string;
  suggestedVocabulary?: string[];
  minWords?: number;
}

export interface CurriculumLesson {
  id: string;
  title: string;
  order: number;
  summary?: string;
  lessonSteps?: Array<{
    id: string; type: 'introduction' | 'vocabulary' | 'grammar' | 'listening' | 'speaking' | 'reading' | 'writing' | 'quiz' | 'recap';
    title: string; order: number; required: boolean; activityRefs: string[]; completionPolicy: { kind: string };
  }>;
  flashcards: FlashcardItem[];
  listeningExercises: ListeningItem[];
  speakingExercises: SpeakingItem[];
  readingArticles?: ReadingItem[];
  writingPrompts?: WritingItem[];
  grammarTopics: GrammarItem[];
  quizQuestions: QuizQuestionItem[];
}

export interface CurriculumUnit {
  id: string;
  title: string;
  order: number;
  lessons: CurriculumLesson[];
}

export interface CurriculumCourse {
  id: string;
  languageCode: string; // 'en', 'ja', 'zh', 'ko', 'fr', 'de'
  level: string;        // 'A1', 'A2', 'B1', 'N5', 'N4', 'HSK 1', 'HSK 2'...
  domain: string;       // 'general', 'it', 'business', 'travel', 'academic'
  title: string;
  description: string;
  units: CurriculumUnit[];
}

class CurriculumStoreService {
  private courses: CurriculumCourse[] = [];

  public replaceFromServer(courses: CurriculumCourse[]): void {
    this.courses = courses.map((course) => ({
      ...course,
      units: sortUnits(course.units || []).map((unit) => ({
        ...unit,
        lessons: sortLessons(unit.lessons || [])
      }))
    }));
  }

  // --- GETTERS ---
  public getAllCourses(): CurriculumCourse[] {
    return [...this.courses];
  }

  public getFilteredCourses(langCode?: string, level?: string, domain?: string): CurriculumCourse[] {
    return this.courses.filter((c) => {
      if (langCode && langCode !== 'all' && c.languageCode.toLowerCase() !== langCode.toLowerCase()) {
        return false;
      }
      if (level && level !== 'all' && c.level.toLowerCase() !== level.toLowerCase()) {
        return false;
      }
      if (domain && domain !== 'all' && c.domain.toLowerCase() !== domain.toLowerCase()) {
        return false;
      }
      return true;
    });
  }

  public getCourseById(id: string): CurriculumCourse | undefined {
    return this.courses.find((c) => c.id === id);
  }

  // --- SKILL GETTERS SYNCHRONIZED WITH TARGET/CURRICULUM ---
  public getListeningForTarget(target: { languageCode?: string; level?: string; domain?: string; domains?: string[] } | null, _filterLang?: string, filterDomain?: string, _filterLevel?: string): (ListeningItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] {
    const list: (ListeningItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] = [];
    if (!target?.languageCode || !target.level) return list;
    const allowedDomains = (target.domains?.length ? target.domains : [target.domain || 'general']).map((domain) => domain.toLowerCase());

    const matchedCourses = this.courses.filter((c) => {
      if (c.languageCode.toLowerCase() !== target.languageCode?.toLowerCase()) return false;
      if (!allowedDomains.includes(c.domain.toLowerCase())) return false;
      if (filterDomain && filterDomain !== 'all' && c.domain.toLowerCase() !== filterDomain.toLowerCase()) return false;
      if (c.level.toLowerCase() !== target.level?.toLowerCase()) return false;
      return true;
    });

    matchedCourses.forEach((c) => {
      c.units.forEach((u) => {
        u.lessons.forEach((l) => {
          (l.listeningExercises || []).forEach((item) => {
            list.push({
              ...item,
              courseId: c.id,
              courseTitle: c.title,
              level: c.level,
              domain: c.domain,
              languageCode: c.languageCode
            });
          });
        });
      });
    });

    return list;
  }

  public getGrammarForTarget(target: { languageCode?: string; level?: string; domain?: string; domains?: string[] } | null, _filterLang?: string, filterDomain?: string, _filterLevel?: string): (GrammarItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] {
    const list: (GrammarItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] = [];
    if (!target?.level) return list;

    const effectiveLang = target.languageCode || '';
    const effectiveLevel = target.level;
    const allowedDomains = (target.domains?.length ? target.domains : [target.domain || 'general']).map((domain) => domain.toLowerCase());

    const matchedCourses = this.courses.filter((c) => {
      if (effectiveLang !== 'all' && c.languageCode.toLowerCase() !== effectiveLang.toLowerCase()) return false;
      if (!allowedDomains.includes(c.domain.toLowerCase())) return false;
      if (filterDomain && filterDomain !== 'all' && allowedDomains.includes(filterDomain.toLowerCase()) && c.domain.toLowerCase() !== filterDomain.toLowerCase()) return false;
      if (effectiveLevel !== 'all' && c.level.toLowerCase() !== effectiveLevel.toLowerCase()) return false;
      return true;
    });

    const seen = new Set<string>();
    matchedCourses.forEach((c) => {
      c.units.forEach((u) => {
        u.lessons.forEach((l) => {
          (l.grammarTopics || []).forEach((item) => {
            const key = `${c.languageCode}|${c.level}|${c.domain}|${item.title.trim().toLocaleLowerCase()}`;
            if (seen.has(key)) return;
            seen.add(key);
            const source = item as GrammarItem & { examples?: string[]; practicePrompt?: string };
            list.push({
              ...item,
              courseId: c.id,
              formula: source.formula && source.formula !== source.title ? source.formula : '',
              example: source.example || source.examples?.[0] || '',
              question: source.question || source.practicePrompt || '',
              courseTitle: c.title,
              level: c.level,
              domain: c.domain,
              languageCode: c.languageCode
            });
          });
        });
      });
    });

    return list;
  }

  public getSpeakingForTarget(target: { languageCode?: string; level?: string; domain?: string; domains?: string[] } | null, _filterLang?: string, filterDomain?: string, _filterLevel?: string): (SpeakingItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] {
    const list: (SpeakingItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] = [];
    if (!target?.languageCode || !target.level) return list;
    const allowedDomains = (target.domains?.length ? target.domains : [target.domain || 'general']).map((domain) => domain.toLowerCase());

    const matchedCourses = this.courses.filter((c) => {
      if (c.languageCode.toLowerCase() !== target.languageCode?.toLowerCase()) return false;
      if (!allowedDomains.includes(c.domain.toLowerCase())) return false;
      if (filterDomain && filterDomain !== 'all' && c.domain.toLowerCase() !== filterDomain.toLowerCase()) return false;
      if (c.level.toLowerCase() !== target.level?.toLowerCase()) return false;
      return true;
    });

    matchedCourses.forEach((c) => {
      c.units.forEach((u) => {
        u.lessons.forEach((l) => {
          (l.speakingExercises || []).forEach((item) => {
            list.push({
              ...item,
              courseId: c.id,
              courseTitle: c.title,
              level: c.level,
              domain: c.domain,
              languageCode: c.languageCode
            });
          });
        });
      });
    });

    return list;
  }

  public getReadingForTarget(target: { languageCode?: string; level?: string; domain?: string; domains?: string[] } | null, _filterLang?: string, filterDomain?: string, _filterLevel?: string): (ReadingItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] {
    const list: (ReadingItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] = [];
    if (!target?.level) return list;

    const effectiveLang = target.languageCode || '';
    const effectiveLevel = target.level;
    const allowedDomains = (target.domains?.length ? target.domains : [target.domain || 'general']).map((domain) => domain.toLowerCase());

    const matchedCourses = this.courses.filter((c) => {
      if (effectiveLang !== 'all' && c.languageCode.toLowerCase() !== effectiveLang.toLowerCase()) return false;
      if (!allowedDomains.includes(c.domain.toLowerCase())) return false;
      if (filterDomain && filterDomain !== 'all' && allowedDomains.includes(filterDomain.toLowerCase()) && c.domain.toLowerCase() !== filterDomain.toLowerCase()) return false;
      if (effectiveLevel !== 'all' && c.level.toLowerCase() !== effectiveLevel.toLowerCase()) return false;
      return true;
    });

    matchedCourses.forEach((c) => {
      c.units.forEach((u) => {
        u.lessons.forEach((l) => {
          (l.readingArticles || []).forEach((item) => {
            list.push({
              ...item,
              courseId: c.id,
              courseTitle: c.title,
              level: c.level,
              domain: c.domain,
              languageCode: c.languageCode
            });
          });
        });
      });
    });

    return list;
  }

  public getWritingForTarget(target: { languageCode?: string; level?: string; domain?: string; domains?: string[] } | null, _filterLang?: string, filterDomain?: string, _filterLevel?: string): (WritingItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] {
    const list: (WritingItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[] = [];
    if (!target?.level) return list;

    const effectiveLang = target.languageCode || '';
    const effectiveLevel = target.level;
    const allowedDomains = (target.domains?.length ? target.domains : [target.domain || 'general']).map((domain) => domain.toLowerCase());

    const matchedCourses = this.courses.filter((c) => {
      if (effectiveLang !== 'all' && c.languageCode.toLowerCase() !== effectiveLang.toLowerCase()) return false;
      if (!allowedDomains.includes(c.domain.toLowerCase())) return false;
      if (filterDomain && filterDomain !== 'all' && allowedDomains.includes(filterDomain.toLowerCase()) && c.domain.toLowerCase() !== filterDomain.toLowerCase()) return false;
      if (effectiveLevel !== 'all' && c.level.toLowerCase() !== effectiveLevel.toLowerCase()) return false;
      return true;
    });

    matchedCourses.forEach((c) => {
      c.units.forEach((u) => {
        u.lessons.forEach((l) => {
          (l.writingPrompts || []).forEach((item) => {
            list.push({
              ...item,
              courseId: c.id,
              courseTitle: c.title,
              level: c.level,
              domain: c.domain,
              languageCode: c.languageCode
            });
          });
        });
      });
    });

    return list;
  }

  // --- COMPREHENSIVE IMPORT / EXPORT ---
  public importCurriculumFromJson(jsonContent: string, replaceExisting = false): { success: boolean; count: number; message: string } {
    try {
      const parsed = JSON.parse(jsonContent);
      const incomingCourses: CurriculumCourse[] = Array.isArray(parsed) ? parsed : [parsed];

      if (incomingCourses.length === 0 || !incomingCourses[0].title) {
        return { success: false, count: 0, message: 'Dữ liệu JSON không đúng định dạng Giáo trình.' };
      }

      // Automatically sort units and lessons naturally for each incoming course
      incomingCourses.forEach((c) => {
        c.units = sortUnits(c.units || []).map((u) => ({
          ...u,
          lessons: sortLessons(u.lessons || [])
        }));
      });

      if (replaceExisting) {
        this.courses = incomingCourses;
      } else {
        // Merge or replace duplicate IDs
        const existingIds = new Set(this.courses.map((c) => c.id));
        const newCourses = [...this.courses];
        incomingCourses.forEach((inc) => {
          if (existingIds.has(inc.id)) {
            const idx = newCourses.findIndex((c) => c.id === inc.id);
            if (idx >= 0) newCourses[idx] = inc;
          } else {
            newCourses.push(inc);
          }
        });
        this.courses = newCourses;
      }

        return {
        success: true,
        count: incomingCourses.length,
        message: `Đã nhập thành công ${incomingCourses.length} khóa học vào hệ thống giáo trình!`
      };
    } catch (e: any) {
      return { success: false, count: 0, message: 'Lỗi cú pháp JSON: ' + (e?.message || 'Không thể đọc tệp') };
    }
  }

}

export const curriculumStore = new CurriculumStoreService();

