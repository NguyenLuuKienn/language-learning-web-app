import type { CourseDetail } from '../types';
import type { CourseMaterial } from './courseMaterial';

export type SpecialPracticeContent = Pick<CourseMaterial, 'certificateExams' | 'interviewScenarios'>;

export function createSpecialPracticeTemplate(course: CourseDetail): SpecialPracticeContent {
  const level = course.level;
  const certificateExams: NonNullable<CourseMaterial['certificateExams']> = course.languageCode?.toLowerCase() === 'en' ? [
    {
      id: crypto.randomUUID(), title: `IELTS ${level} practice sample`, certificate: 'IELTS', level, durationMinutes: 30,
      sections: [
        { skill: 'listening', questions: [{ id: crypto.randomUUID(), prompt: 'Where will the speakers meet?',
          audioScript: 'A: Let us meet at the library. B: Great, see you there.',
          options: ['At the library', 'At the station', 'At the cafe'], correctIndex: 0, points: 1 }] },
        { skill: 'reading', questions: [{ id: crypto.randomUUID(), prompt: 'When does the library open?',
          passage: 'The library opens at nine every weekday.',
          options: ['At eight', 'At nine', 'At ten'], correctIndex: 1, points: 1 }] },
        { skill: 'writing', questions: [{ id: crypto.randomUUID(), prompt: 'Write about a place you like visiting.',
          rubric: 'Assess task response, coherence, vocabulary and grammar for the stated level.', points: 10 }] },
        { skill: 'speaking', questions: [{ id: crypto.randomUUID(), prompt: 'Describe your hometown.',
          rubric: 'Assess relevance, fluency, vocabulary, grammar and pronunciation from recorded audio.', points: 10 }] }
      ]
    },
    {
      id: crypto.randomUUID(), title: `TOEIC Listening & Reading ${level} practice sample`, certificate: 'TOEIC', level, durationMinutes: 20,
      sections: [
        { skill: 'listening', questions: [{ id: crypto.randomUUID(), prompt: 'Where is the meeting?',
          audioScript: 'The meeting will be in room five at two o’clock.',
          options: ['Room three', 'Room five', 'Room seven'], correctIndex: 1, points: 1 }] },
        { skill: 'reading', questions: [{ id: crypto.randomUUID(), prompt: 'When is the office closed?',
          passage: 'The office is open Monday through Friday and closed on weekends.',
          options: ['On Friday', 'On Saturday', 'On Monday'], correctIndex: 1, points: 1 }] }
      ]
    },
    {
      id: crypto.randomUUID(), title: `TOEIC Speaking & Writing ${level} practice sample`, certificate: 'TOEIC', level,
      sections: [
        { skill: 'speaking', questions: [{ id: crypto.randomUUID(), prompt: 'Describe a typical workday.',
          rubric: 'Assess task completion, clarity, language use and pronunciation from recorded audio.', points: 10 }] },
        { skill: 'writing', questions: [{ id: crypto.randomUUID(), prompt: 'Write a short email to arrange a meeting.',
          rubric: 'Assess task completion, organization, vocabulary and grammar.', points: 10 }] }
      ]
    }
  ] : [];
  return {
    certificateExams,
    interviewScenarios: [{
      id: crypto.randomUUID(), title: `Job interview practice ${level}`, level,
      profession: course.domain || 'general',
      rubric: 'Assess relevance, role-specific vocabulary, clear structure and communication. Assess pronunciation only from recorded audio.',
      turns: [
        { id: crypto.randomUUID(), prompt: 'Please introduce yourself and describe your experience.' },
        { id: crypto.randomUUID(), prompt: 'Why are you interested in this role?' },
        { id: crypto.randomUUID(), prompt: 'Describe a challenge you solved.' }
      ]
    }]
  };
}
