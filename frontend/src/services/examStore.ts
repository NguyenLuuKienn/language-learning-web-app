
import type { CourseMaterial } from './courseMaterial';

export interface ExamQuestion {
  id: string;
  languageCode: string; // 'en', 'ja', 'zh', 'ko', 'fr', 'de'
  level: string;        // 'A1', 'A2', 'B1', 'B2', 'N5', 'N4', 'HSK 1', 'HSK 2'...
  section: 'listening' | 'grammar_vocab' | 'reading' | 'writing' | 'speaking';
  prompt: string;
  audioScript?: string; // For listening section TTS
  readingPassage?: string; // For reading section
  options?: string[]; // For multiple choice (Listening, Grammar, Reading)
  correctIndex?: number; // 0-based index for multiple choice
  sampleAnswer?: string; // For writing or open-ended
  expectedResponse?: string;
  minWords?: number; // For writing
  explanation: string;
  points: number;
}

export interface ExamResult {
  examId: string;
  attemptId?: string;
  status?: 'Pending' | 'Passed' | 'Failed';
  feedback?: string;
  languageCode: string;
  targetLevel: string;
  totalPoints: number;
  earnedPoints: number;
  percentage: number;
  passed: boolean;
  sectionBreakdown: {
    listening: { earned: number; total: number };
    speaking: { earned: number; total: number };
    grammar_vocab: { earned: number; total: number };
    reading: { earned: number; total: number };
    writing: { earned: number; total: number };
  };
  attemptedAt: string;
}

class ExamStoreService {
  private questions: ExamQuestion[] = [];

  public replaceFromServer(materials: CourseMaterial[]): void {
    this.questions = materials.flatMap((material) => material.placementTests.flatMap((test) =>
      test.questions.map((question, index) => ({
        id: `${material.id}-${test.level}-${index}`,
        languageCode: material.languageCode,
        level: test.level,
        section: question.section,
        prompt: question.prompt,
        audioScript: question.audioScript,
        readingPassage: question.readingPassage,
        expectedResponse: question.expectedResponse,
        options: question.options,
        correctIndex: question.correctIndex,
        minWords: question.minWords,
        explanation: question.rubric,
        points: question.points
      }))
    ));
  }

  public upsertCourseMaterial(material: CourseMaterial): void {
    const existing = this.questions.filter((question) => !question.id.startsWith(`${material.id}-`));
    this.replaceFromServer([material]);
    this.questions = [...existing, ...this.questions];
  }

  public getQuestionsForExam(languageCode: string, level: string): ExamQuestion[] {
    const lCode = (languageCode || 'en').toLowerCase();
    const lvl = (level || 'A1').toUpperCase();

    const matched = this.questions.filter(
      (q) => q.languageCode.toLowerCase() === lCode && q.level.toUpperCase() === lvl
    );

    return matched;
  }

}

export const examStore = new ExamStoreService();
