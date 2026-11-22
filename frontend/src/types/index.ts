export type UserRole = 'Learner' | 'Admin' | number;

export const isAdminUser = (user: UserProfile | null | undefined): boolean => {
  if (!user) return false;
  const roleVal = String(user.role ?? '').trim().toLowerCase();
  return roleVal === 'admin' || roleVal === '2';
};

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  timezone: string;
  avatarUrl?: string;
  dailyGoal: number;
  currentStreak: number;
}

export interface AuthResponse {
  expiresAt: string;
  user: UserProfile;
}

export interface Language {
  id: string;
  code: string;
  name: string;
  nativeName?: string;
  flagEmoji?: string;
}

export interface Course {
  id: string;
  languageId: string;
  languageCode?: string;
  title: string;
  level: string;
  domain?: string;
  description: string;
  status: 'Draft' | 'Published';
  unitsCount: number;
  totalLessons: number;
  hasMaterial?: boolean;
}

export interface CourseDetail extends Course {
  units: UnitDetail[];
}

export interface UnitDetail {
  id: string;
  courseId: string;
  title: string;
  sortOrder: number;
  lessons: LessonSummary[];
}

export interface LessonSummary {
  id: string;
  title: string;
  sortOrder: number;
  lessonType?: string;
  status: 'NotStarted' | 'InProgress' | 'Completed';
  vocabCount: number;
}

export interface LessonDetail {
  id: string;
  unitId: string;
  courseId: string;
  title: string;
  sortOrder: number;
  lessonType?: string;
  status: 'NotStarted' | 'InProgress' | 'Completed';
  vocabularies: Vocabulary[];
}

export interface Vocabulary {
  id: string;
  lessonId: string;
  term: string;
  phonetic: string;
  meaning: string;
  meanings?: string[];
  partOfSpeech?: string;
  example: string;
  audioUrl?: string;
  imageUrl?: string;
}

export interface Deck {
  id: string;
  ownerId: string;
  languageId: string;
  languageCode?: string;
  title: string;
  description: string;
  visibility: 'Private' | 'Public';
  isPublic?: boolean;
  totalCards: number;
  dueCards: number;
  newCards: number;
  tags: string[];
  version: number;
}

export interface Flashcard {
  id: string;
  deckId: string;
  front: string;
  back: string;
  phonetic?: string;
  partOfSpeech?: string;
  meanings?: string[];
  example?: string;
  audioUrl?: string;
  imageUrl?: string;
  version: number;
  schedule?: { dueAt: string; intervalDays: number; repetitions: number } | null;
}

export type SrsRating = 'Again' | 'Hard' | 'Good' | 'Easy';

export interface CardSchedule {
  id: string;
  cardId: string;
  dueAt: string;
  intervalDays: number;
  easeFactor: number;
  repetitions: number;
  lapses: number;
  algorithmVersion: string;
}

export interface ReviewEvent {
  clientEventId: string;
  cardId: string;
  rating: SrsRating;
  reviewedAt: string;
  algorithmVersion: string;
}

export interface ReviewResult {
  cardId: string;
  nextDueAt: string;
  intervalDays: number;
  easeFactor: number;
  repetitions: number;
}

export interface DueCardsSummary {
  dueCount: number;
  cards: Flashcard[];
}

export type QuestionType = 'MultipleChoice' | 'FillInBlank' | 'Matching';

export interface QuestionOption {
  id: string;
  content: string;
  sortOrder: number;
}

export interface QuizQuestion {
  id: string;
  lessonId: string;
  type: QuestionType;
  prompt: string;
  explanation: string;
  options: QuestionOption[];
  matchingTerms?: string[];
  matchingChoices?: string[];
}

export interface QuizAnswerSubmission {
  questionId: string;
  selectedOptionId?: string;
  textAnswer?: string;
  matchingJson?: string;
}

export interface QuizResult {
  attemptId: string;
  lessonId: string;
    score: number;
    maxScore: number;
    percentage: number;
  passed: boolean;
    completedAt: string;
}

export interface CourseProgress {
  courseId: string;
  completedLessons: number;
  totalLessons: number;
  percentComplete: number;
  status: string;
}

export interface CsvPreviewItem {
  rowNumber: number;
  front: string;
  back: string;
  tags: string[];
  isValid: boolean;
  errorMessage?: string;
}

export interface CsvPreviewResponse {
  totalRows: number;
  validRows: number;
  invalidRows: number;
  items: CsvPreviewItem[];
}

export interface CsvImportResult {
  insertedCount: number;
  skippedCount: number;
  message: string;
}

export interface StatisticsOverview {
  totalCardsReviewed?: number;
  totalReviews?: number;
  cardsMastered?: number;
  cardsLearning?: number;
  currentStreakDays?: number;
  currentStreak?: number;
  retentionRatePercentage?: number;
  retentionRate?: number;
  todayReviewsCount?: number;
  dailyGoal?: number;
  dailyTarget?: number;
  goalMetToday?: boolean;
}

export interface HeatmapDay {
  date: string;
  reviewCount: number;
  lessonsCompleted: number;
  level: number;
}

export interface PublicDeck {
  id: string;
  languageId: string;
  title: string;
  description: string;
  ownerDisplayName: string;
  totalCards?: number;
  cardsCount?: number;
  tags: string[];
  cloneCount?: number;
  publishedAt?: string;
  createdAt?: string;
}

export interface PublicDeckPreview extends PublicDeck {
  sampleCards: Flashcard[];
}

export interface SyncOperation {
  operationId: string;
  type: string;
  entityId: string;
  payload: Record<string, unknown>;
  occurredAt: string;
  status: 'Pending' | 'Synced' | 'Failed';
}

export interface SyncOperationResult {
  operationId: string;
  status: 'Accepted' | 'Rejected';
  message: string;
}

export interface SyncPushResponse {
  processedCount: number;
  results: SyncOperationResult[];
}

export interface SyncPullResponse {
  serverTimestamp: string;
  updatedDecks: Deck[];
  updatedCards: Flashcard[];
  newCursor: string;
}

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface AuditLog {
  id: string;
  userId: string;
  action: string;
  targetEntity: string;
  targetId?: string;
  details: string;
  ipAddress: string;
  createdAt: string;
}

export interface StudyTarget {
  languageId: string;
  languageCode: string;
  languageName: string;
  nativeName?: string;
  flagEmoji?: string;
  level: string;
  domain: string;
  domains?: string[];
    placementProof?: string;
}

  export interface PlacementQuestion {
    id: string;
    section: 'listening' | 'speaking' | 'reading' | 'writing';
    prompt: string;
    audioScript?: string;
    readingPassage?: string;
    options?: string[];
    minWords?: number;
    points: number;
  }

  export interface PlacementExam {
    courseId: string;
    languageCode: string;
    level: string;
    questions: PlacementQuestion[];
  }

  export interface PlacementResult {
    attemptId: string;
    status: 'Pending' | 'Passed' | 'Failed';
    feedback: string;
    courseId: string;
    languageCode: string;
    targetLevel: string;
    totalPoints: number;
    earnedPoints: number;
    percentage: number;
    passed: boolean;
    sectionBreakdown: Record<string, { earned: number; total: number }>;
    attemptedAt: string;
    placementProof?: string;
  }

export interface LeaderboardEntry {
  userId: string;
  displayName: string;
  reviewCount: number;
  lessonsCompleted: number;
  xp: number;
  currentStreak: number;
}

export interface CreateLanguagePayload {
  code: string;
  name: string;
  nativeName?: string;
  flagEmoji?: string;
}

export interface UpdateLanguagePayload {
  name: string;
  nativeName?: string;
  flagEmoji?: string;
}

export interface CurriculumVocabItem {
  term: string;
  phonetic?: string;
  meaning: string;
  meanings?: string[];
  partOfSpeech?: string;
  example?: string;
  audioUrl?: string;
  imageUrl?: string;
}

export interface CurriculumLessonItem {
  title: string;
  sortOrder: number;
  lessonType?: string;
  vocabularies: CurriculumVocabItem[];
}

export interface CurriculumUnitItem {
  title: string;
  sortOrder: number;
  lessons: CurriculumLessonItem[];
}

export interface CurriculumCourseItem {
  languageCode: string;
  title: string;
  level: string;
  domain?: string;
  description?: string;
  units: CurriculumUnitItem[];
}

export interface CurriculumExportResponse {
  exportedAt: string;
  languages: Language[];
  courses: CurriculumCourseItem[];
}

export interface CurriculumImportResultResponse {
  coursesCreated: number;
  unitsCreated: number;
  lessonsCreated: number;
  vocabulariesCreated: number;
  message: string;
}

export interface TopicWritingPrompt {
  id: string;
  title: string;
  domain: string;
  level: string;
  scenario: string;
  instruction: string;
  suggestedVocabulary: string[];
  minWords: number;
}

export interface SpeakingReadingItem {
  id: string;
  title: string;
  domain: string;
  level: string;
  targetText: string;
  phonetic?: string;
  translation: string;
  contextNote?: string;
}
