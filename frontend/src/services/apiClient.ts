import type {
  AdminUser,
  AuditLog,
  AuthResponse,
  CardSchedule,
  Course,
  CourseDetail,
  CsvImportResult,
  CsvPreviewResponse,
  Deck,
  DueCardsSummary,
  Flashcard,
  HeatmapDay,
  LessonDetail,
  PublicDeck,
  PublicDeckPreview,
  QuizAnswerSubmission,
  QuizQuestion,
  QuizResult,
  ReviewResult,
  SrsRating,
  StatisticsOverview,
  StudyTarget,
  SyncPullResponse,
  SyncPushResponse,
  UserProfile,
  UserRole,
  Vocabulary,
  Language,
  LeaderboardEntry,
  PlacementExam,
  PlacementResult,
  CreateLanguagePayload,
  UpdateLanguagePayload,
  CurriculumExportResponse
} from '../types';
import { localizeApiError } from './errorMessages';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

export class ApiError extends Error {
  status: number;
  data: unknown;

  constructor(status: number, message: string, data?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.data = data;
  }
}

export function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError && typeof error.data === 'object' && error.data !== null) {
    const code = (error.data as { code?: unknown }).code;
    if (typeof code === 'string') {
      const localized = localizeApiError(code);
      if (localized) return localized;
    }
  }
  return error instanceof Error && error.message.trim() ? error.message : fallback;
}

async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const csrf = document.cookie.split('; ').find((part) => part.startsWith('ll_csrf='))?.slice('ll_csrf='.length);
  const headers: HeadersInit = {
    ...(!(options.body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}),
    ...(csrf && options.method && options.method !== 'GET' ? { 'X-CSRF-Token': decodeURIComponent(csrf) } : {}),
    ...options.headers
  };

  let response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
    credentials: 'same-origin'
  });
  if (response.status === 401 && !endpoint.startsWith('/auth/')) {
    const renewed = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: 'POST', credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', ...(csrf ? { 'X-CSRF-Token': decodeURIComponent(csrf) } : {}) },
      body: '{}'
    });
    if (renewed.ok) {
      const nextCsrf = document.cookie.split('; ').find((part) => part.startsWith('ll_csrf='))?.slice('ll_csrf='.length);
      response = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...options, credentials: 'same-origin',
        headers: { ...headers, ...(nextCsrf ? { 'X-CSRF-Token': decodeURIComponent(nextCsrf) } : {}) }
      });
    }
  }

  if (!response.ok) {
    let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
    let errorData: unknown;

    try {
      errorData = await response.json();
      if (typeof errorData === 'object' && errorData !== null) {
        const problem = errorData as { message?: unknown; detail?: unknown; title?: unknown; errors?: Record<string, unknown> };
        const fieldError = problem.errors && Object.entries(problem.errors)
          .flatMap(([field, messages]) => Array.isArray(messages) ? messages.map((item) => `${field}: ${String(item)}`) : [])
          .find(Boolean);
        errorMessage = fieldError ?? String(problem.message ?? problem.detail ?? problem.title ?? errorMessage);
      }
    } catch {
    }

    throw new ApiError(response.status, errorMessage, errorData);
  }

  if (options.method && options.method !== 'GET' && /^\/admin\/(courses|curriculum)(\/|$)/.test(endpoint)) {
    window.dispatchEvent(new Event('curriculum:updated'));
  }

  if (response.status === 204) {
    return null as T;
  }

  return response.json();
}

export const authApi = {
  logout: () => apiRequest<{ message: string }>('/auth/logout', { method: 'POST', body: '{}' }),
  login: (data: { email: string; password: string }) =>
    apiRequest<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  register: (data: { email: string; password: string; displayName: string }) =>
    apiRequest<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  getMe: () => apiRequest<UserProfile>('/me'),

  updateProfile: (data: { displayName?: string; avatarUrl?: string }) =>
    apiRequest<UserProfile>('/me', {
      method: 'PUT',
      body: JSON.stringify(data)
    }),

  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    apiRequest<{ message: string }>('/me/change-password', {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  updateDailyGoal: (dailyTarget: number) =>
    apiRequest<{ dailyTarget: number }>('/me/daily-goal', {
      method: 'PUT',
      body: JSON.stringify({ dailyTarget })
    }),

  updateTimezone: (timezone: string) => apiRequest<UserProfile>('/me', {
    method: 'PUT', body: JSON.stringify({ timezone })
  })
};

export const placementApi = {
  getExam: (courseId: string) => apiRequest<PlacementExam>(`/placement/courses/${courseId}`),
  uploadAudio: (courseId: string, questionId: string, audio: Blob) => {
    const body = new FormData();
    body.append('file', audio, 'speaking.webm');
    return apiRequest<{ fileName: string; url: string }>(`/placement/courses/${courseId}/audio/${encodeURIComponent(questionId)}`, {
      method: 'POST', body
    });
  },
  submit: (courseId: string, selectedOptions: Record<string, number>, writtenAnswers: Record<string, string>, audioFileNames: Record<string, string>) =>
    apiRequest<PlacementResult>(`/placement/courses/${courseId}/submit`, {
      method: 'POST',
      body: JSON.stringify({ selectedOptions, writtenAnswers, audioFileNames })
    }),
  getResult: (attemptId: string) => apiRequest<PlacementResult>(`/placement/attempts/${attemptId}`),
  getLatestResult: (courseId: string) => apiRequest<PlacementResult | null>(`/placement/courses/${courseId}/latest`),
  getPendingReviews: () => apiRequest<PlacementReview[]>(`/placement/admin/pending`),
  review: (attemptId: string, scores: Record<string, number>, feedback: string) =>
    apiRequest<PlacementResult>(`/placement/admin/attempts/${attemptId}/review`, {
      method: 'POST', body: JSON.stringify({ scores, feedback })
    })
};

export interface PlacementReview {
  attemptId: string;
  courseId: string;
  userId: string;
  learnerName: string;
  courseTitle: string;
  submittedAt: string;
  questions: Array<{
    id: string; section: string; prompt: string; rubric?: string; expectedResponse?: string;
    writtenAnswer?: string; audioUrl?: string; points: number;
  }>;
}

export const studyTargetApi = {
  get: () => apiRequest<StudyTarget | null>('/me/study-target'),
  save: (target: StudyTarget) => apiRequest<StudyTarget>('/me/study-target', {
    method: 'PUT',
    body: JSON.stringify(target)
  })
};

export interface WritingSubmission {
  id: string;
  promptId: string;
  text: string;
  createdAt: string;
}

export const writingApi = {
  getLatest: (promptId: string) => apiRequest<WritingSubmission | null>(`/me/writing-submissions/latest?promptId=${encodeURIComponent(promptId)}`),
  save: (promptId: string, text: string, clientSubmissionId: string = crypto.randomUUID()) => apiRequest<WritingSubmission>('/me/writing-submissions', {
    method: 'POST',
    body: JSON.stringify({ promptId, text, clientSubmissionId })
  })
};

export const coursesApi = {
  getLanguages: () => apiRequest<Language[]>('/languages'),

  getCourses: async (languageId?: string, level?: string, domain?: string, signal?: AbortSignal) => {
    if (languageId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(languageId)) {
      const languageCode = languageId.replace(/^lang-/i, '').replace(/-lang$/i, '');
      const languages = await apiRequest<Language[]>('/languages', { signal });
      languageId = languages.find((item) => item.code.toLowerCase() === languageCode.toLowerCase())?.id;
      if (!languageId) return [];
    }
    const params = new URLSearchParams();
    if (languageId) params.append('languageId', languageId);
    if (level) params.append('level', level);
    if (domain) params.append('domain', domain);
    const qs = params.toString();
    const courses = await apiRequest<Array<Course & { lessonsCount?: number }>>(qs ? `/courses?${qs}` : '/courses', { signal });
    return courses.map((course) => ({ ...course, totalLessons: course.lessonsCount ?? course.totalLessons ?? 0 }));
  },

  getCourseDetail: async (courseId: string) => {
    const course = await apiRequest<CourseDetail & { lessonsCount?: number }>(`/courses/${courseId}`);
    return { ...course, totalLessons: course.lessonsCount ?? course.totalLessons ?? 0 };
  },

  getCourseMaterial: (courseId: string, signal?: AbortSignal) =>
    apiRequest<unknown>(`/courses/${courseId}/material`, { signal }),

  getLessonDetail: (lessonId: string) =>
    apiRequest<LessonDetail>(`/lessons/${lessonId}`),

  getVocabularies: (lessonId: string) =>
    apiRequest<Vocabulary[]>(`/lessons/${lessonId}/vocabularies`)
};

export const dictionaryApi = {
  search: (term: string, languageId?: string) => {
    const params = new URLSearchParams({ term });
    if (languageId) params.set('languageId', languageId);
    return apiRequest<Array<Vocabulary & { languageId: string }>>(`/vocabularies?${params}`);
  }
};

export const decksApi = {
  getDecks: async () => {
    const first = await apiRequest<{ items: Deck[]; totalPages: number }>('/decks?page=1&pageSize=50');
    const rest = await Promise.all(Array.from({ length: Math.max(0, first.totalPages - 1) }, (_, index) =>
      apiRequest<{ items: Deck[] }>(`/decks?page=${index + 2}&pageSize=50`)
    ));
    return [first, ...rest].flatMap((page) => page.items).map((deck) => ({
      ...deck,
      visibility: (deck.isPublic ? 'Public' : 'Private') as Deck['visibility']
    }));
  },

  createDeck: (deck: { title: string; description: string; languageId: string; isPublic: boolean; tags: string[] }) =>
    apiRequest<Deck>('/decks', { method: 'POST', body: JSON.stringify(deck) }),

  deleteDeck: (deckId: string) =>
    apiRequest<void>(`/decks/${deckId}`, { method: 'DELETE' }),

  createCardsBatch: (deckId: string, cards: Array<{ front: string; back: string }>) =>
    apiRequest<Flashcard[]>(`/decks/${deckId}/cards/batch`, { method: 'POST', body: JSON.stringify({ cards }) }),

  getCardsByDeckId: (deckId: string) =>
    apiRequest<Flashcard[]>(`/decks/${deckId}/cards`),

  createCard: (deckId: string, card: { front: string; back: string; phonetic?: string; partOfSpeech?: string; meanings?: string[]; example?: string; tags?: string[] }) =>
    apiRequest<Flashcard>(`/decks/${deckId}/cards`, {
      method: 'POST',
      body: JSON.stringify(card)
    }),

  updateCard: (cardId: string, card: { front: string; back: string; tags?: string[] }) =>
    apiRequest<Flashcard>(`/cards/${cardId}`, {
      method: 'PUT',
      body: JSON.stringify(card)
    }),

  deleteCard: (cardId: string) =>
    apiRequest<void>(`/cards/${cardId}`, {
      method: 'DELETE'
    })
};

export const srsApi = {
  getLessonSources: () => apiRequest<{ lessonId: string; lessonTitle: string; unitTitle: string; cardCount: number }[]>('/reviews/lessons'),
  getOverview: (deckId?: string, lessonId?: string) => {
    const params = new URLSearchParams();
    if (deckId) params.set('deckId', deckId);
    if (lessonId) params.set('lessonId', lessonId);
    return apiRequest<{ dueCount: number; learningCount: number; unseenCount: number; reviewCount: number; relearningCount: number; dueForecast7Days: number[]; totalCount: number; observedDueReviews: number; observedRecallPercent: number | null; nextDueAt?: string | null }>(`/reviews/overview?${params}`);
  },
  getDueCards: (deckId?: string, limit: number = 50, dueOnly = false, newOnly = false, lessonId?: string) => {
    const params = new URLSearchParams({ limit: String(limit), dueOnly: String(dueOnly), newOnly: String(newOnly) });
    if (deckId) params.set('deckId', deckId);
    if (lessonId) params.set('lessonId', lessonId);
    return apiRequest<DueCardsSummary>(`/reviews/due?${params}`);
  },

  submitReview: (cardId: string, rating: SrsRating, durationMs: number = 0) =>
    apiRequest<ReviewResult>('/reviews', {
      method: 'POST',
      body: JSON.stringify({
        cardId,
        rating,
        durationMs,
        clientEventId: crypto.randomUUID(),
        reviewedAt: new Date().toISOString()
      })
    }),

  getReviewHistory: (limit: number = 50) =>
    apiRequest<CardSchedule[]>(`/reviews/history?limit=${limit}`)
};

export interface NotebookEntry {
  id: string;
  text: string;
  note: string;
  languageCode: string;
  sourcePath: string;
  context: string;
  createdAt: string;
  updatedAt?: string;
}

export type NotebookEntryInput = Omit<NotebookEntry, 'id' | 'createdAt' | 'updatedAt'>;

export const notebookApi = {
  list: (search = '', page = 1) =>
    apiRequest<{ items: NotebookEntry[]; total: number; page: number; pageSize: number }>(
      '/notebook?search=' + encodeURIComponent(search) + '&page=' + page + '&pageSize=20'
    ),
  create: (entry: NotebookEntryInput) =>
    apiRequest<NotebookEntry>('/notebook', { method: 'POST', body: JSON.stringify(entry) }),
  update: (id: string, entry: NotebookEntryInput) =>
    apiRequest<NotebookEntry>('/notebook/' + id, { method: 'PUT', body: JSON.stringify(entry) }),
  remove: (id: string) =>
    apiRequest<void>('/notebook/' + id, { method: 'DELETE' })
};

export interface VocabularyLessonChoice {
  courseId: string;
  unitId: string;
  lessonId: string;
  unitSortOrder: number;
  lessonSortOrder: number;
  lessonTitle: string;
  unitTitle: string;
  courseTitle: string;
  availableGroups: number;
  completedGroups: number;
  nextGroupWords?: string[];
  activeSessionId?: string | null;
  activePosition: number;
  activeTotalSteps: number;
}

export interface VocabularyPracticeStep {
  id: string;
  kind: 'introduction' | 'listenMeaning' | 'cloze' | 'matching' | 'dictation';
  round: number;
  word: string;
  meaning: string;
  meanings: string[];
  phonetic: string;
  partOfSpeech: string;
  example: string;
  exampleTranslation: string;
  prompt: string;
  audioUrl: string;
  options: string[];
  matchWords: string[];
  matchMeanings: string[];
}

export interface VocabularySession {
  id: string;
  lessonId: string;
  groupIndex: number;
  position: number;
  totalSteps: number;
  correctCount: number;
  words?: string[];
  completed: boolean;
  canCompleteAfterRoundOne: boolean;
  step: VocabularyPracticeStep | null;
}

export const vocabularySessionsApi = {
  lessons: () => apiRequest<VocabularyLessonChoice[]>('/vocabulary-sessions/lessons'),
  start: (lessonId: string) =>
    apiRequest<VocabularySession>('/vocabulary-sessions', {
      method: 'POST', body: JSON.stringify({ lessonId })
    }),
  get: (sessionId: string) => apiRequest<VocabularySession>('/vocabulary-sessions/' + sessionId),
  completePerfectRound: (sessionId: string) => apiRequest<VocabularySession>(
    '/vocabulary-sessions/' + sessionId + '/complete-perfect-round', { method: 'POST' }
  ),
  answer: (sessionId: string, stepId: string, answer = '', matches?: Record<string, string>) =>
    apiRequest<{ correct: boolean; expected: string; session: VocabularySession }>(
      '/vocabulary-sessions/' + sessionId + '/answers',
      { method: 'POST', body: JSON.stringify({ stepId, answer, matches }) }
    )
};

export interface SpecialPracticeCatalogItem {
  courseId: string; contentId: string; kind: 'certificate' | 'interview'; title: string;
  languageCode: string; level: string; domain: string; certificate: string; profession: string;
  questionCount: number; activeAttemptId: string | null; lastAttemptId: string | null; lastStatus: string | null; archived?: boolean;
}
export interface SpecialPracticeQuestion {
  id: string; skill: string; prompt: string; passage: string; audioScript: string;
  audioUrl: string; options: string[]; points: number;
  rubric: string;
}
export interface SpecialPracticeAnswer { selectedIndex: number | null; text: string | null; audioUrl: string | null }
export interface SpecialPracticeAttempt {
  id: string; courseId: string; contentId: string; kind: 'certificate' | 'interview';
  title: string; certificate: string; profession: string; status: string;
  questions: SpecialPracticeQuestion[]; answers: Record<string, SpecialPracticeAnswer>;
  objectiveScore: number; objectiveMaxScore: number; submittedAt: string | null;
  subjectiveScore: number; subjectiveMaxScore: number; feedback: string; reviewedAt: string | null;
  deadlineAt: string | null;
}
export interface SpecialPracticeReviewItem { attemptId: string; userId: string; kind: string; title: string; submittedAt: string }
export interface SpecialPracticeReviewDetail {
  attemptId: string; kind: string; title: string; rubric: string;
  questions: SpecialPracticeQuestion[]; answers: Record<string, SpecialPracticeAnswer>;
  objectiveScore: number; objectiveMaxScore: number;
}
export const specialPracticeApi = {
  catalog: () => apiRequest<SpecialPracticeCatalogItem[]>('/special-practice/catalog'),
  start: (courseId: string, contentId: string, kind: 'certificate' | 'interview') =>
    apiRequest<SpecialPracticeAttempt>('/special-practice/attempts', {
      method: 'POST', body: JSON.stringify({ courseId, contentId, kind })
    }),
  get: (attemptId: string) => apiRequest<SpecialPracticeAttempt>(`/special-practice/attempts/${attemptId}`),
  answer: (attemptId: string, questionId: string, selectedIndex: number | null, text: string | null) =>
    apiRequest<SpecialPracticeAttempt>(`/special-practice/attempts/${attemptId}/answers`, {
      method: 'PUT', body: JSON.stringify({ questionId, selectedIndex, text })
    }),
  uploadAudio: (attemptId: string, questionId: string, blob: Blob) => {
    const form = new FormData(); form.append('file', blob, 'response.webm');
    return apiRequest<SpecialPracticeAttempt>(`/special-practice/attempts/${attemptId}/questions/${questionId}/audio`, {
      method: 'POST', body: form
    });
  },
  submit: (attemptId: string) => apiRequest<SpecialPracticeAttempt>(
    `/special-practice/attempts/${attemptId}/submit`, { method: 'POST' }),
  pendingReviews: () => apiRequest<SpecialPracticeReviewItem[]>('/special-practice/reviews/pending'),
  review: (attemptId: string) => apiRequest<SpecialPracticeReviewDetail>(`/special-practice/reviews/${attemptId}`),
  grade: (attemptId: string, scores: Record<string, number>, feedback: string) =>
    apiRequest<SpecialPracticeAttempt>(`/special-practice/reviews/${attemptId}`, {
      method: 'POST', body: JSON.stringify({ scores, feedback })
    })
};

export interface ResumeLearning {
  courseId: string;
  courseTitle: string;
  lessonId: string;
  lessonTitle: string;
  lastAccessedAt: string;
  stepId?: string | null;
  activityId?: string | null;
  cursor?: number;
}

export interface LessonJourneyStep {
  id: string; type: string; title: string; order: number; required: boolean;
  completionPolicy: string; activityRefs: string[]; status: 'Locked' | 'Available' | 'InProgress' | 'Completed';
  activityId: string | null; cursor: number; revision: number; draftJson: string;
}
export interface LessonJourney {
  courseId: string; unitId: string; lessonId: string; courseTitle: string;
  unitTitle: string; lessonTitle: string; summary: string; status: string;
  contentVersion: string; contentChangedSinceStart: boolean;
  weakPoints: { stepType: string; activityId: string; prompt: string; incorrectAttempts: number }[];
  currentStepId: string; completedRequiredSteps: number; totalRequiredSteps: number;
  steps: LessonJourneyStep[];
}
export const lessonJourneyApi = {
  get: (lessonId: string) => apiRequest<LessonJourney>(`/learning-journey/lessons/${lessonId}`),
  refresh: (lessonId: string) => apiRequest<LessonJourney>(`/learning-journey/lessons/${lessonId}/refresh`, { method: 'POST' }),
    visit: (lessonId: string, stepId: string, value: { activityId?: string; cursor?: number; draftJson?: string; completeIntroduction?: boolean; expectedRevision: number; contentVersion: string }) =>
    apiRequest<LessonJourney>(`/learning-journey/lessons/${lessonId}/steps/${encodeURIComponent(stepId)}`, {
      method: 'PUT', body: JSON.stringify({ cursor: 0, ...value })
    })
};

export interface LearningRoadmap {
  totalLessons: number;
  completedLessons: number;
  nextCourseId: string | null;
  nextLessonId: string | null;
  courses: Array<{
    id: string; title: string; level: string; domain: string;
    totalLessons: number; completedLessons: number;
    units: Array<{ id: string; title: string; sortOrder: number;
      lessons: Array<{ id: string; title: string; lessonType: string; sortOrder: number;
        status: string; lastAccessedAt: string | null }> }>;
  }>;
}

export const progressApi = {
  getResume: () => apiRequest<ResumeLearning | null>('/progress/resume'),
  getRoadmap: () => apiRequest<LearningRoadmap>('/progress/roadmap'),
  getLesson: (lessonId: string) => apiRequest<{ status: string }>(`/progress/lessons/${lessonId}`),
  updateLesson: (lessonId: string, status: 'InProgress' | 'Completed') =>
    apiRequest<void>(`/progress/lessons/${lessonId}`, { method: 'PATCH', body: JSON.stringify({ status }) })
};

export type SkillKind = 'listening' | 'speaking' | 'reading' | 'writing' | 'grammar';
export interface SkillPracticeState {
  courseId: string;
  activityId: string;
  status: 'InProgress' | 'Practiced' | 'Completed';
  attempts: number;
  correctAttempts: number;
  correct: boolean | null;
  updatedAt: string;
}
export const skillPracticeApi = {
  list: (kind: SkillKind) => apiRequest<SkillPracticeState[]>(`/skill-practice/${kind}`),
  record: (kind: SkillKind, courseId: string, activityId: string, answerIndex?: number, practiced = false, clientAttemptId: string = crypto.randomUUID()) =>
    apiRequest<SkillPracticeState>(`/skill-practice/${kind}`, {
      method: 'POST', body: JSON.stringify({ courseId, activityId, answerIndex, practiced, clientAttemptId })
    })
};

export const quizApi = {
  getLessonQuiz: (lessonId: string) =>
    apiRequest<QuizQuestion[]>(`/lessons/${lessonId}/quiz`),

  submitQuiz: (lessonId: string, answers: QuizAnswerSubmission[], clientAttemptId: string) =>
    apiRequest<QuizResult>('/quiz-attempts', {
      method: 'POST',
      body: JSON.stringify({
        lessonId,
        clientAttemptId,
        answers
      })
    })
};

export const statisticsApi = {
  getOverview: () => apiRequest<StatisticsOverview>('/statistics/overview'),
  getHeatmap: () => apiRequest<HeatmapDay[]>('/statistics/heatmap'),
  getLeaderboard: () => apiRequest<LeaderboardEntry[]>('/statistics/leaderboard')
};

export const sharingApi = {
  getPublicDecks: (search?: string, languageId?: string) => {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    if (languageId) params.set('languageId', languageId);
    const url = `/public/decks${params.size ? `?${params}` : ''}`;
    return apiRequest<PublicDeck[]>(url);
  },

  getPublicDeckPreview: (deckId: string) =>
    apiRequest<PublicDeckPreview>(`/public/decks/${deckId}/preview`),

  cloneDeck: (deckId: string) =>
    apiRequest<Deck>(`/public/decks/${deckId}/clone`, {
      method: 'POST'
    })
};

export const csvApi = {
  previewCsv: (deckId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiRequest<CsvPreviewResponse>(`/decks/${deckId}/imports/preview`, {
      method: 'POST',
      body: formData
    });
  },

  commitCsvImport: (
    deckId: string,
    cards: Array<{ front: string; back: string; tags: string[] }>
  ) =>
    apiRequest<CsvImportResult>(`/decks/${deckId}/imports`, {
      method: 'POST',
      body: JSON.stringify({ cards })
    }),

  getExportUrl: (deckId: string) => `${API_BASE_URL}/decks/${deckId}/export`
};

export const syncApi = {
  pushOperations: (operations: unknown[]) =>
    apiRequest<SyncPushResponse>('/sync/push', {
      method: 'POST',
      body: JSON.stringify({ operations })
    }),

  pullChanges: (cursor?: string) => {
    const url = cursor ? `/sync/pull?cursor=${encodeURIComponent(cursor)}` : '/sync/pull';
    return apiRequest<SyncPullResponse>(url);
  }
};

export interface CourseCategory {
  id: string;
  kind: 'level' | 'domain';
  framework: '' | 'cefr' | 'jlpt' | 'hsk' | 'topik';
  code: string;
  titleVi: string;
  titleEn: string;
  descriptionVi: string;
  descriptionEn: string;
  sortOrder: number;
}

export type CourseCategoryPayload = Omit<CourseCategory, 'id'>;

export const courseCategoriesApi = {
  list: () => apiRequest<CourseCategory[]>('/course-categories'),
  create: (payload: CourseCategoryPayload) => apiRequest<CourseCategory>('/course-categories', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id: string, payload: CourseCategoryPayload) => apiRequest<CourseCategory>(`/course-categories/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  delete: (id: string) => apiRequest<void>(`/course-categories/${id}`, { method: 'DELETE' })
};

export const adminApi = {
  getUsers: (search?: string) => {
    const url = search ? `/admin/users?search=${encodeURIComponent(search)}` : '/admin/users';
    return apiRequest<AdminUser[]>(url);
  },

  updateUserStatus: (userId: string, isActive: boolean) =>
    apiRequest<AdminUser>(`/admin/users/${userId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ isActive })
    }),

  updateUserRole: (userId: string, role: UserRole) =>
    apiRequest<AdminUser>(`/admin/users/${userId}/role`, {
      method: 'PATCH',
      body: JSON.stringify({ role })
    }),

  getAuditLogs: () => apiRequest<AuditLog[]>('/admin/audit-logs'),

  createLanguage: (payload: CreateLanguagePayload) =>
    apiRequest<Language>('/admin/languages', {
      method: 'POST',
      body: JSON.stringify(payload)
    }),

  updateLanguage: (id: string, payload: UpdateLanguagePayload) =>
    apiRequest<Language>(`/admin/languages/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    }),

  deleteLanguage: (id: string) =>
    apiRequest<void>(`/admin/languages/${id}`, {
      method: 'DELETE'
    }),

  exportCurriculum: (languageId?: string) => {
    const url = languageId ? `/admin/curriculum/export?languageId=${languageId}` : '/admin/curriculum/export';
    return apiRequest<CurriculumExportResponse>(url);
  },

  createCourse: (payload: {
    languageId: string;
    title: string;
    level: string;
    domain: string;
    description: string;
    status?: number;
  }) =>
    apiRequest<Course>('/admin/courses', {
      method: 'POST',
      body: JSON.stringify(payload)
    }),

  updateCourse: (id: string, payload: { title: string; level: string; domain: string; description: string; status: 'Draft' | 'Published' }) =>
    apiRequest<Course>(`/admin/courses/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    }),

  importCourseMaterial: (courseId: string, material: unknown) =>
    apiRequest<void>(`/admin/courses/${courseId}/material`, {
      method: 'PUT',
      body: JSON.stringify(material)
    }),

  updateSpecialPractice: (courseId: string, content: unknown) =>
    apiRequest<void>(`/admin/courses/${courseId}/special-practice`, {
      method: 'PUT', body: JSON.stringify(content)
    }),

  listSpecialPractice: (courseId: string, kind: 'certificate' | 'interview') =>
    apiRequest<Record<string, unknown>[]>(`/admin/courses/${courseId}/special-practice/${kind}`),
  createSpecialPractice: (courseId: string, kind: 'certificate' | 'interview', item: unknown) =>
    apiRequest<unknown>(`/admin/courses/${courseId}/special-practice/${kind}`, { method: 'POST', body: JSON.stringify(item) }),
  importSpecialPractice: (courseId: string, kind: 'certificate' | 'interview', items: unknown[]) =>
    apiRequest<unknown>(`/admin/courses/${courseId}/special-practice/${kind}/import`, { method: 'POST', body: JSON.stringify(items) }),
  editSpecialPractice: (courseId: string, kind: 'certificate' | 'interview', id: string, item: unknown) =>
    apiRequest<unknown>(`/admin/courses/${courseId}/special-practice/${kind}/${id}`, { method: 'PUT', body: JSON.stringify(item) }),
  deleteSpecialPractice: (courseId: string, kind: 'certificate' | 'interview', id: string) =>
    apiRequest<void>(`/admin/courses/${courseId}/special-practice/${kind}/${id}`, { method: 'DELETE' }),

  updateLessonMaterial: (courseId: string, lessonId: string, lesson: import('./curriculumStore').CurriculumLesson) =>
    apiRequest<void>(`/admin/courses/${courseId}/lessons/${lessonId}/material`, {
      method: 'PUT',
      body: JSON.stringify(lesson)
    }),
  remapLessonSteps: (courseId: string, lessonId: string, expectedContentVersion: string, stepIds: Record<string, string>) =>
    apiRequest<void>(`/admin/courses/${courseId}/lessons/${lessonId}/remap-steps`, {
      method: 'POST', body: JSON.stringify({ expectedContentVersion, stepIds })
    }),
  getSrsEvaluationReadiness: () => apiRequest<{
    totalReviews: number; distinctLearners: number; distinctCards: number;
    firstReviewAt: string | null; lastReviewAt: string | null;
    dueReviewsWithInterval: number; dueReviewsRecalled: number;
    missingDueFlag: number; missingPreviousInterval: number;
    reviewsByAlgorithm: Record<string, number>;
  }>('/admin/srs/evaluation-readiness'),

  getPlacementTests: (courseId: string) =>
    apiRequest<import('./courseMaterial').CourseMaterial['placementTests']>(`/admin/courses/${courseId}/placement-tests`),

  updatePlacementTests: (courseId: string, tests: import('./courseMaterial').CourseMaterial['placementTests']) =>
    apiRequest<void>(`/admin/courses/${courseId}/placement-tests`, {
      method: 'PUT',
      body: JSON.stringify(tests)
    }),

  deleteCourse: (id: string) =>
    apiRequest<void>(`/admin/courses/${id}`, {
      method: 'DELETE'
    }),

  createUnit: (courseId: string, payload: { title: string; sortOrder: number }) =>
    apiRequest<any>(`/admin/courses/${courseId}/units`, {
      method: 'POST',
      body: JSON.stringify(payload)
    }),

  deleteUnit: (unitId: string) =>
    apiRequest<void>(`/admin/courses/units/${unitId}`, {
      method: 'DELETE'
    }),

  createLesson: (
    unitId: string,
    payload: { title: string; lessonType?: string; sortOrder?: number; content?: string }
  ) =>
    apiRequest<any>(`/admin/courses/units/${unitId}/lessons`, {
      method: 'POST',
      body: JSON.stringify(payload)
    }),

  deleteLesson: (lessonId: string) =>
    apiRequest<void>(`/admin/courses/lessons/${lessonId}`, {
      method: 'DELETE'
    })
};
