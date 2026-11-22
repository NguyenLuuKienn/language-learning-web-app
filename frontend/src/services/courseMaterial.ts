import type { CourseDetail, Language } from '../types';
import type { CurriculumCourse } from './curriculumStore';

export interface CourseMaterial extends CurriculumCourse {
  certificateExams?: Array<{
    id: string; title: string; certificate: 'IELTS' | 'TOEIC'; level: string; durationMinutes?: number;
    sections: Array<{ skill: 'listening' | 'reading' | 'speaking' | 'writing'; questions: Array<{
      id: string; prompt: string; points: number; audioScript?: string; audioUrl?: string;
      passage?: string; options?: string[]; correctIndex?: number; rubric?: string;
    }> }>;
  }>;
  interviewScenarios?: Array<{
    id: string; title: string; level: string; profession: string; rubric: string;
    turns: Array<{ id: string; prompt: string; context?: string }>;
  }>;
  decks: Array<{ title: string; description: string; cards: Array<{ front: string; back: string; phonetic?: string; partOfSpeech?: string; meanings?: string[]; example?: string; exampleTranslation?: string; clozeSentence?: string; acceptedAnswers?: string[]; audioUrl?: string; imageUrl?: string; tags: string[] }> }>;
  srs: { algorithm: 'srs-v1'; dailyGoal: number };
  placementTests: Array<{
    level: string;
    questions: Array<{
      section: 'listening' | 'speaking' | 'reading' | 'writing';
      prompt: string;
      audioScript?: string;
      readingPassage?: string;
      expectedResponse?: string;
      options?: string[];
      correctIndex?: number;
      minWords?: number;
      rubric: string;
      points: number;
    }>;
  }>;
}

function validateVocabularyCard(card: { front: string; back: string; phonetic?: string; partOfSpeech?: string; meanings?: string[]; example?: string; exampleTranslation?: string; clozeSentence?: string; acceptedAnswers?: string[] }, path: string): void {
  if (typeof card.front !== 'string' || !card.front.trim()) throw new Error(`${path}.front không được để trống.`);
  if (typeof card.back !== 'string' || !card.back.trim()) throw new Error(`${path}.back không được để trống.`);
  if (card.meanings !== undefined && (!Array.isArray(card.meanings) || card.meanings.length < 1 || card.meanings.length > 20 || card.meanings.some((meaning) => typeof meaning !== 'string' || !meaning.trim() || meaning.length > 300))) {
    throw new Error(`${path}.meanings phải có 1–20 nghĩa, mỗi nghĩa tối đa 300 ký tự.`);
  }
  for (const [field, maxLength] of [['phonetic', 100], ['partOfSpeech', 60], ['example', 2000], ['exampleTranslation', 2000], ['clozeSentence', 2000]] as const) {
    const value = card[field];
    if (value !== undefined && (typeof value !== 'string' || value.length > maxLength)) throw new Error(`${path}.${field} không hợp lệ.`);
  }
  if (card.clozeSentence !== undefined || card.acceptedAnswers !== undefined) {
    if (!card.clozeSentence?.includes('___')) throw new Error(path + '.clozeSentence phải chứa ___ để đánh dấu chỗ trống.');
    if (!/\p{L}/u.test(card.clozeSentence.replaceAll('___', ''))) throw new Error(path + '.clozeSentence cần một câu có ngữ cảnh, không chỉ có ___.');
    if (!Array.isArray(card.acceptedAnswers) || card.acceptedAnswers.length < 1 || card.acceptedAnswers.length > 10 ||
      card.acceptedAnswers.some((answer) => typeof answer !== 'string' || !answer.trim() || answer.length > 100)) {
      throw new Error(path + '.acceptedAnswers cần 1–10 đáp án hợp lệ.');
    }
  }
}

export function createCourseMaterialTemplate(course: CourseDetail, language: Language): CourseMaterial {
  const exampleCards = language.code.toLowerCase() === 'en' ? [
    { front: 'apply', back: 'nộp đơn', meanings: ['nộp đơn', 'áp dụng'], phonetic: '/əˈplaɪ/', partOfSpeech: 'verb', example: 'I want to apply for this job.', exampleTranslation: 'Tôi muốn nộp đơn cho công việc này.', clozeSentence: 'I want to ___ for this job.' },
    { front: 'experience', back: 'kinh nghiệm', meanings: ['kinh nghiệm', 'trải nghiệm'], phonetic: '/ɪkˈspɪəriəns/', partOfSpeech: 'noun', example: 'She has experience in customer service.', exampleTranslation: 'Cô ấy có kinh nghiệm trong lĩnh vực chăm sóc khách hàng.', clozeSentence: 'She has ___ in customer service.' },
    { front: 'skill', back: 'kỹ năng', meanings: ['kỹ năng'], phonetic: '/skɪl/', partOfSpeech: 'noun', example: 'Communication is an important skill.', exampleTranslation: 'Giao tiếp là một kỹ năng quan trọng.', clozeSentence: 'Communication is an important ___.' }
  ].map((card) => ({ id: crypto.randomUUID(), ...card, acceptedAnswers: [card.front], audioUrl: '', imageUrl: '', tags: ['career', 'interview'] })) : [{
    id: crypto.randomUUID(), front: '', back: '', phonetic: '', partOfSpeech: '', meanings: [''],
    example: '', exampleTranslation: '', clozeSentence: '___', acceptedAnswers: [''], audioUrl: '', imageUrl: '', tags: []
  }];
  return {
    id: course.id,
    languageCode: language.code,
    level: course.level,
    domain: course.domain ?? 'general',
    title: course.title,
    description: course.description,
    units: [{
      id: crypto.randomUUID(),
      title: '',
      order: 1,
      lessons: [{
        id: crypto.randomUUID(),
        title: '',
        order: 1,
        lessonSteps: [
          { id: 'introduction', type: 'introduction', title: 'Giới thiệu', order: 0, required: true, activityRefs: [], completionPolicy: { kind: 'viewed' } },
          { id: 'vocabulary', type: 'vocabulary', title: 'Từ vựng', order: 1, required: true, activityRefs: [], completionPolicy: { kind: 'session_completed' } }
        ],
        flashcards: exampleCards,
        listeningExercises: [],
        speakingExercises: [],
        readingArticles: [],
        writingPrompts: [],
        grammarTopics: [],
        quizQuestions: []
      }]
    }],
    decks: [],
    certificateExams: [],
    interviewScenarios: [],
    srs: { algorithm: 'srs-v1', dailyGoal: 10 },
    placementTests: [{ level: course.level, questions: [
      { section: 'listening', prompt: '', audioScript: '', options: ['', ''], correctIndex: 0, rubric: '', points: 25 },
      { section: 'speaking', prompt: '', expectedResponse: '', rubric: '', points: 25 },
      { section: 'reading', prompt: '', readingPassage: '', options: ['', ''], correctIndex: 0, rubric: '', points: 25 },
      { section: 'writing', prompt: '', expectedResponse: '', minWords: 30, rubric: '', points: 25 }
    ] }]
  };
}

export function parseCourseMaterial(value: string, course: CourseDetail): CourseMaterial {
  const material: unknown = JSON.parse(value);
  if (!material || typeof material !== 'object' || Array.isArray(material)) throw new Error('JSON khóa học không hợp lệ.');
  const data = material as Partial<CourseMaterial>;
  if (data.id !== course.id) {
    throw new Error(`ID khóa học trong JSON là ${data.id ?? '(thiếu)'}, còn khóa học đang chọn là ${course.id}. Hãy tải JSON mẫu của đúng khóa học hoặc thay id ở đầu file bằng ID đang chọn.`);
  }
  if (data.level?.toUpperCase() !== course.level.toUpperCase()) {
    throw new Error(`Trình độ trong JSON là ${data.level ?? '(thiếu)'}, còn khóa học đang chọn là ${course.level}.`);
  }
  if (data.domain?.toLowerCase() !== (course.domain ?? 'general').toLowerCase()) {
    throw new Error(`Lĩnh vực trong JSON là ${data.domain ?? '(thiếu)'}, còn khóa học đang chọn là ${course.domain ?? 'general'}.`);
  }
  if (course.languageCode && data.languageCode?.toLowerCase() !== course.languageCode.toLowerCase()) {
    throw new Error(`languageCode phải là ${course.languageCode} để khớp khóa học đã tạo.`);
  }
  if (!Array.isArray(data.units) || !Array.isArray(data.decks) || !Array.isArray(data.placementTests) || !data.srs) {
    throw new Error('JSON phải có units, decks, srs và placementTests.');
  }
  if (course.units?.length) {
    throw new Error('Khóa học đã có Unit. Hiện chỉ có thể import JSON trước khi thêm Unit hoặc Lesson.');
  }
  if (data.units.length === 0) throw new Error('units phải có ít nhất một Unit.');
  if (data.srs.algorithm !== 'srs-v1') throw new Error('srs.algorithm phải là srs-v1.');
  if (data.placementTests.length === 0) throw new Error('placementTests phải có bài kiểm tra đầu vào.');
  const requiredText = (input: unknown, path: string): void => {
    if (typeof input !== 'string' || !input.trim()) throw new Error(`${path} không được để trống.`);
  };
  const requiredOptions = (input: unknown, index: number, path: string): void => {
    if (!Array.isArray(input) || input.length < 2 || input.some((option) => typeof option !== 'string' || !option.trim())) {
      throw new Error(`${path}.options cần ít nhất 2 lựa chọn có nội dung.`);
    }
    if (!Number.isInteger(index) || index < 0 || index >= input.length) {
      throw new Error(`${path}.correctIndex không hợp lệ.`);
    }
  };
  for (const [testIndex, test] of data.placementTests.entries()) {
    const path = `placementTests[${testIndex}]`;
    if (!Array.isArray(test.questions)) throw new Error(`${path}.questions phải là danh sách.`);
    const skills = new Set(test.questions.map((question) => question.section));
    for (const skill of ['listening', 'speaking', 'reading', 'writing']) {
      if (!skills.has(skill as CourseMaterial['placementTests'][number]['questions'][number]['section'])) {
        throw new Error(`${path}.questions thiếu phần ${skill}.`);
      }
    }
    test.questions.forEach((question, index) => {
      const questionPath = `${path}.questions[${index}]`;
      requiredText(question.prompt, `${questionPath}.prompt`);
      requiredText(question.rubric, `${questionPath}.rubric`);
      if (!Number.isInteger(question.points) || question.points < 1 || question.points > 100) {
        throw new Error(`${questionPath}.points must be between 1 and 100.`);
      }
      if (question.section === 'listening') requiredText(question.audioScript, `${questionPath}.audioScript`);
      if (question.section === 'reading') requiredText(question.readingPassage, `${questionPath}.readingPassage`);
      if (question.section === 'speaking' || question.section === 'writing') requiredText(question.expectedResponse, `${questionPath}.expectedResponse`);
      if (question.section === 'listening' || question.section === 'reading') requiredOptions(question.options, question.correctIndex ?? -1, questionPath);
    });
  }
  const sections = ['flashcards', 'listeningExercises', 'speakingExercises', 'readingArticles', 'writingPrompts', 'grammarTopics', 'quizQuestions'] as const;
  const totals = Object.fromEntries(sections.map((section) => [section, 0])) as Record<typeof sections[number], number>;
  for (const [unitIndex, unit] of data.units.entries()) {
    requiredText(unit.title, `units[${unitIndex}].title`);
    if (!Array.isArray(unit.lessons)) throw new Error('Mỗi Unit phải có lessons.');
    for (const [lessonIndex, lesson] of unit.lessons.entries()) {
      const path = `units[${unitIndex}].lessons[${lessonIndex}]`;
      requiredText(lesson.title, `${path}.title`);
      for (const section of sections) {
        if (!Array.isArray(lesson[section])) throw new Error(`${path}.${section} phải là danh sách.`);
        totals[section] += lesson[section].length;
      }
      if (lesson.lessonSteps !== undefined) {
        if (!Array.isArray(lesson.lessonSteps) || !lesson.lessonSteps.length) throw new Error(`${path}.lessonSteps cần ít nhất một bước.`);
        const source: Record<string, keyof typeof lesson> = { vocabulary: 'flashcards', grammar: 'grammarTopics', listening: 'listeningExercises', speaking: 'speakingExercises', reading: 'readingArticles', writing: 'writingPrompts', quiz: 'quizQuestions' };
        const policies: Record<string, string> = { introduction: 'viewed', vocabulary: 'session_completed', grammar: 'all_correct', listening: 'all_correct', speaking: 'practiced', reading: 'all_correct', writing: 'submission_required', quiz: 'quiz_passed', recap: 'viewed' };
        const ids = new Set<string>();
        for (const [index, step] of lesson.lessonSteps.entries()) {
          const stepPath = `${path}.lessonSteps[${index}]`;
          if (!step || typeof step.id !== 'string' || !step.id.trim() || ids.has(step.id) || !(step.type in policies) ||
            !Number.isInteger(step.order) || step.order < 0 || step.completionPolicy?.kind !== policies[step.type] ||
            !Array.isArray(step.activityRefs)) throw new Error(`${stepPath} không hợp lệ.`);
          ids.add(step.id);
          const section = source[step.type];
          if (section) {
            const entries = lesson[section];
            const available = new Set((Array.isArray(entries) ? entries : []).map(entry => entry.id));
            if (!available.size || step.activityRefs.some(id => !available.has(id)) ||
              (!['vocabulary', 'quiz'].includes(step.type) && !step.activityRefs.length))
              throw new Error(`${stepPath}.activityRefs phải trỏ đến nội dung trong bài.`);
          }
        }
        if (!lesson.lessonSteps.some(step => step.type === 'introduction' && step.required !== false))
          throw new Error(`${path}.lessonSteps cần bước giới thiệu bắt buộc.`);
        for (const [type, section] of Object.entries(source)) {
          const entries = lesson[section];
          if (Array.isArray(entries) && entries.length && !lesson.lessonSteps.some(step => step.type === type))
            throw new Error(`${path}.lessonSteps thiếu bước ${type} cho nội dung đã có.`);
        }
      }
      lesson.speakingExercises.forEach((exercise) => {
        const source = exercise as typeof exercise & { prompt?: string; expectedResponse?: string };
        source.title ||= source.prompt || lesson.title;
        source.phrase ||= source.expectedResponse || source.prompt || '';
        source.translation ||= source.prompt || '';
      });
      lesson.grammarTopics.forEach((topic) => {
        const source = topic as typeof topic & { examples?: string[]; practicePrompt?: string };
        source.formula ||= source.title;
        source.example ||= source.examples?.[0] || '';
        source.exampleTranslation ||= '';
        source.question ||= source.practicePrompt || '';
        source.options ||= [];
      });
      lesson.writingPrompts?.forEach((prompt) => {
        const source = prompt as typeof prompt & { prompt?: string };
        source.instruction ||= source.prompt || '';
      });
      lesson.flashcards.forEach((card, index) => {
        validateVocabularyCard(card, `${path}.flashcards[${index}]`);
      });
      lesson.readingArticles?.forEach((article, index) => {
        const articlePath = `${path}.readingArticles[${index}]`;
        requiredText(article.title, `${articlePath}.title`);
        if (!Array.isArray(article.content) || !article.content.length || article.content.some((paragraph) => typeof paragraph !== 'string' || !paragraph.trim())) {
          throw new Error(`${articlePath}.content cần ít nhất một đoạn văn.`);
        }
        if (!Array.isArray(article.vocabularies)) throw new Error(`${articlePath}.vocabularies phải là danh sách.`);
        requiredText(article.quiz?.question, `${articlePath}.quiz.question`);
        requiredOptions(article.quiz?.options, article.quiz?.correctIndex ?? -1, `${articlePath}.quiz`);
      });
      lesson.writingPrompts?.forEach((prompt, index) => {
        const promptPath = `${path}.writingPrompts[${index}]`;
        requiredText(prompt.title, `${promptPath}.title`);
        requiredText(prompt.scenario, `${promptPath}.scenario`);
        requiredText(prompt.instruction, `${promptPath}.instruction`);
      });
      lesson.quizQuestions.forEach((question, index) => {
        const questionPath = `${path}.quizQuestions[${index}]`;
        requiredText(question.question, `${questionPath}.question`);
        requiredOptions(question.options, question.correctIndex, questionPath);
      });
    }
  }
  for (const section of sections) if (!totals[section]) throw new Error(`Khóa học thiếu nội dung ${section}.`);
  return data as CourseMaterial;
}
