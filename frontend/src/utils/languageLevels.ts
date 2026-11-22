import type { CourseCategory } from '../services/apiClient';

export interface LanguageLevelOption {
  code: string;
  title: string;
  desc: string;
  titleEn?: string;
  descEn?: string;
}

export interface DomainOption {
  code: string;
  title: string;
  desc: string;
  titleEn: string;
  descEn: string;
}

export const DOMAINS: DomainOption[] = [
  {
    code: 'general',
    title: 'Giao tiếp Đời sống',
    desc: 'Hội thoại thường ngày, xã hội, văn hóa và du lịch',
    titleEn: 'General Communication',
    descEn: 'Everyday conversations, society, culture and travel'
  },
  {
    code: 'it',
    title: 'CNTT & Kỹ thuật Phần mềm',
    desc: 'Báo cáo lỗi, quy trình agile, review mã nguồn & kiến trúc hệ thống',
    titleEn: 'IT & Software Engineering',
    descEn: 'Bug reports, agile sprints, code review & system architecture'
  },
  {
    code: 'business',
    title: 'Kinh doanh & Thương mại',
    desc: 'Đàm phán hợp đồng, báo cáo tài chính & trao đổi đối tác',
    titleEn: 'Business & Commerce',
    descEn: 'Negotiations, financial reports & partner communications'
  },
  {
    code: 'travel',
    title: 'Du lịch & Khách sạn',
    desc: 'Sân bay, đặt phòng khách sạn, ẩm thực & chỉ đường',
    titleEn: 'Travel & Hospitality',
    descEn: 'Airports, hotel reservations, dining & directions'
  },
  {
    code: 'academic',
    title: 'Học thuật & Nghiên cứu',
    desc: 'Viết luận văn, tài liệu khoa học & chuẩn bị du học',
    titleEn: 'Academic & Research',
    descEn: 'Essay writing, scientific literature & study abroad preparation'
  }
];

let managedLevels: CourseCategory[] | null = null;

export function applyCourseCategories(categories: CourseCategory[]): void {
  managedLevels = categories.filter(item => item.kind === 'level');
  DOMAINS.splice(0, DOMAINS.length, ...categories.filter(item => item.kind === 'domain').map(item => ({
    code: item.code, title: item.titleVi, titleEn: item.titleEn,
    desc: item.descriptionVi, descEn: item.descriptionEn
  })));
}

export function getFrameworkForLanguage(langCode: string): 'cefr' | 'jlpt' | 'hsk' | 'topik' {
  const code = langCode.toLowerCase();
  if (code === 'ja' || code === 'jp' || code.includes('japan')) return 'jlpt';
  if (code === 'zh' || code === 'cn' || code.includes('chin')) return 'hsk';
  if (code === 'ko' || code === 'kr' || code.includes('korea')) return 'topik';
  return 'cefr';
}

export const getDomainTitle = (code: string, lang: 'vi' | 'en' = 'vi'): string => {
  const found = DOMAINS.find((d) => d.code.toLowerCase() === (code || '').toLowerCase());
  if (!found) return code;
  return lang === 'en' ? found.titleEn : found.title;
};

export const getLevelFrameworkName = (langCode: string, lang: 'vi' | 'en' = 'vi'): string => {
  const code = (langCode || '').toLowerCase();
  if (code === 'ja' || code === 'jp' || code.includes('japan')) {
    return lang === 'en' ? 'JLPT (Japan Standard)' : 'JLPT (Nhật Bản)';
  }
  if (code === 'zh' || code === 'cn' || code.includes('chin')) {
    return lang === 'en' ? 'HSK (China Standard)' : 'HSK (Trung Quốc)';
  }
  if (code === 'ko' || code === 'kr' || code.includes('korea')) {
    return lang === 'en' ? 'TOPIK (Korea Standard)' : 'TOPIK (Hàn Quốc)';
  }
  return lang === 'en' ? 'CEFR (European / International Standard)' : 'CEFR (Châu Âu / Quốc tế)';
};

const getBuiltInLevelsForLanguage = (langCode: string, lang: 'vi' | 'en' = 'vi'): LanguageLevelOption[] => {
  const code = (langCode || '').toLowerCase();
  const isEn = lang === 'en';

  // Tiếng Nhật: JLPT N5 -> N1
  if (code === 'ja' || code === 'jp' || code.includes('japan')) {
    return [
      {
        code: 'N5',
        title: isEn ? 'Beginner / Foundation' : 'Nhập môn / Căn bản',
        desc: isEn ? 'Hiragana, Katakana, basic greetings & 800 core words' : 'Bảng chữ Hiragana, Katakana, câu chào hỏi & 800 từ căn bản'
      },
      {
        code: 'N4',
        title: isEn ? 'Elementary' : 'Sơ cấp',
        desc: isEn ? 'Daily routines, compound sentences & 1,500 words' : 'Hội thoại sinh hoạt hàng ngày, câu ghép & 1,500 từ vựng'
      },
      {
        code: 'N3',
        title: isEn ? 'Intermediate' : 'Trung cấp',
        desc: isEn ? 'Workplace conversations, simple news & 3,500 words' : 'Giao tiếp công sở, đọc báo tin tức đơn giản & 3,500 từ vựng'
      },
      {
        code: 'N2',
        title: isEn ? 'Upper Intermediate' : 'Trung cao cấp',
        desc: isEn ? 'Professional business tasks in Japan & 6,000 words' : 'Làm việc chuyên nghiệp trong doanh nghiệp Nhật & 6,000 từ'
      },
      {
        code: 'N1',
        title: isEn ? 'Advanced / Mastery' : 'Thành thạo',
        desc: isEn ? 'Academic literature, high-level business & 10,000 words' : 'Đọc hiểu tài liệu học thuật, đàm phán thương mại & 10,000 từ'
      }
    ];
  }

  // Tiếng Trung: HSK 1 -> HSK 6
  if (code === 'zh' || code === 'cn' || code.includes('chin')) {
    return [
      {
        code: 'HSK 1',
        title: isEn ? 'Level 1 - Starter' : 'Sơ cấp 1',
        desc: isEn ? '150 core words & foundational grammar' : '150 từ vựng & ngữ pháp căn bản nhất cho người mới bắt đầu'
      },
      {
        code: 'HSK 2',
        title: isEn ? 'Level 2 - Elementary' : 'Sơ cấp 2',
        desc: isEn ? '300 words, familiar daily conversations' : '300 từ vựng, giao tiếp những chủ đề thân thuộc hàng ngày'
      },
      {
        code: 'HSK 3',
        title: isEn ? 'Level 3 - Intermediate' : 'Trung cấp 1',
        desc: isEn ? '600 words, independent travel and business communication' : '600 từ vựng, giao tiếp độc lập khi đi du lịch và công tác'
      },
      {
        code: 'HSK 4',
        title: isEn ? 'Level 4 - Upper Inter.' : 'Trung cấp 2',
        desc: isEn ? '1,200 words, fluent discussions on complex topics' : '1,200 từ vựng, thảo luận công việc và các chủ đề chuyên sâu'
      },
      {
        code: 'HSK 5',
        title: isEn ? 'Level 5 - Advanced' : 'Cao cấp 1',
        desc: isEn ? '2,500 words, newspapers, movies and presentations' : '2,500 từ vựng, đọc báo chí, xem phim và thuyết trình'
      },
      {
        code: 'HSK 6',
        title: isEn ? 'Level 6 - Mastery' : 'Thành thạo',
        desc: isEn ? '5,000+ words, near-native spoken & written fluency' : '5,000+ từ vựng, sử dụng tiếng Trung lưu loát như người bản xứ'
      }
    ];
  }

  // Tiếng Hàn: TOPIK 1 -> TOPIK 6
  if (code === 'ko' || code === 'kr' || code.includes('korea')) {
    return [
      {
        code: 'TOPIK 1',
        title: isEn ? 'Level 1 - Starter' : 'Cấp 1 - Mới bắt đầu',
        desc: isEn ? 'Hangul script, self-introductions & simple shopping' : 'Bảng chữ Hangul, giới thiệu bản thân, mua sắm cơ bản'
      },
      {
        code: 'TOPIK 2',
        title: isEn ? 'Level 2 - Elementary' : 'Cấp 2 - Sơ cấp',
        desc: isEn ? 'Everyday phone calls, directions & dining' : 'Giao tiếp hàng ngày qua điện thoại, hỏi đường, ẩm thực'
      },
      {
        code: 'TOPIK 3',
        title: isEn ? 'Level 3 - Intermediate' : 'Cấp 3 - Trung cấp 1',
        desc: isEn ? 'Public utilities & social interactions' : 'Sử dụng các tiện ích công cộng, văn hóa xã hội'
      },
      {
        code: 'TOPIK 4',
        title: isEn ? 'Level 4 - Upper Inter.' : 'Cấp 4 - Trung cấp 2',
        desc: isEn ? 'TV broadcasts, workplace discussions' : 'Xem tin tức truyền hình, thảo luận công sở'
      },
      {
        code: 'TOPIK 5',
        title: isEn ? 'Level 5 - Advanced' : 'Cấp 5 - Nâng cao',
        desc: isEn ? 'Professional research & academic papers' : 'Nghiên cứu chuyên môn, viết văn bản học thuật'
      },
      {
        code: 'TOPIK 6',
        title: isEn ? 'Level 6 - Mastery' : 'Cấp 6 - Thành thạo',
        desc: isEn ? 'Comprehensive fluency & native-level nuance' : 'Lưu loát toàn diện, biểu đạt tinh tế như người bản địa'
      }
    ];
  }

  // Tiếng Anh, Pháp, Đức: CEFR A1 -> C2
  return [
    {
      code: 'A1',
      title: isEn ? 'Beginner' : 'Mới bắt đầu',
      desc: isEn ? 'Basic greetings, pronunciation & everyday phrases' : 'Chào hỏi, phát âm chuẩn, từ vựng nền tảng hàng ngày'
    },
    {
      code: 'A2',
      title: isEn ? 'Elementary' : 'Cơ bản',
      desc: isEn ? 'Daily life conversations, basic tenses & shopping' : 'Hội thoại đời sống, ngữ pháp thì cơ bản, mua sắm'
    },
    {
      code: 'B1',
      title: isEn ? 'Intermediate' : 'Trung cấp',
      desc: isEn ? 'Workplace communication, emails & technical reading' : 'Giao tiếp công sở, viết email, đọc tài liệu chuyên ngành'
    },
    {
      code: 'B2',
      title: isEn ? 'Upper Intermediate' : 'Nâng cao',
      desc: isEn ? 'Technical discussions, presentations & quick reflex' : 'Thảo luận kỹ thuật, thuyết trình, phản xạ tự nhiên'
    },
    {
      code: 'C1',
      title: isEn ? 'Advanced' : 'Thành thạo',
      desc: isEn ? 'Academic nuances & strategic negotiations' : 'Học thuật chuyên sâu, đàm phán chiến lược'
    },
    {
      code: 'C2',
      title: isEn ? 'Mastery' : 'Bản xứ',
      desc: isEn ? 'Effortless fluency & rich stylistic expression' : 'Lưu loát tinh tế, diễn đạt phong phú đa phong cách'
    }
  ];
};

export const getLevelsForLanguage = (langCode: string, lang: 'vi' | 'en' = 'vi'): LanguageLevelOption[] => {
  const builtIn = getBuiltInLevelsForLanguage(langCode, lang);
  if (!managedLevels) return builtIn;
  return managedLevels.filter(item => item.framework === getFrameworkForLanguage(langCode))
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map(item => ({ code: item.code, title: lang === 'vi' ? item.titleVi : item.titleEn,
      desc: (lang === 'vi' ? item.descriptionVi : item.descriptionEn) || builtIn.find(level => level.code === item.code)?.desc || '' }));
};

export const getDefaultLevelForLanguage = (langCode: string): string => {
  const levels = getLevelsForLanguage(langCode);
  return levels[0]?.code || 'A1';
};

const getBuiltInRoadmapLevels = (langCode: string, lang: 'vi' | 'en' = 'vi') => {
  const code = (langCode || '').toLowerCase();
  const isEn = lang === 'en';

  if (code === 'ja' || code === 'jp' || code.includes('japan')) {
    return [
      { code: 'N5', name: isEn ? 'Starter' : 'Nhập môn', focus: isEn ? 'Hiragana/Katakana, greetings, 800 core words' : 'Bảng chữ Hiragana/Katakana, chào hỏi, 800 từ cốt lõi', completed: true },
      { code: 'N4', name: isEn ? 'Elementary' : 'Sơ cấp', focus: isEn ? 'Daily life, Te/Ta grammar, 1,500 words' : 'Sinh hoạt đời sống, ngữ pháp Te/Ta, 1,500 từ vựng', completed: false, current: true },
      { code: 'N3', name: isEn ? 'Intermediate' : 'Trung cấp', focus: isEn ? 'Office communication, news reading, 3,500 words' : 'Giao tiếp công sở, đọc báo tin tức, 3,500 từ vựng', completed: false },
      { code: 'N2', name: isEn ? 'Upper Inter.' : 'Trung cao', focus: isEn ? 'Japanese corporate work, technical specs, 6,000 words' : 'Làm việc doanh nghiệp Nhật, đọc hiểu chuyên ngành, 6,000 từ', completed: false },
      { code: 'N1', name: isEn ? 'Mastery' : 'Thành thạo', focus: isEn ? 'Advanced commerce, negotiations & academia, 10,000 words' : 'Thương mại cao cấp, đàm phán và học thuật, 10,000 từ', completed: false }
    ];
  }
  if (code === 'zh' || code === 'cn' || code.includes('chin')) {
    return [
      { code: 'HSK 1', name: isEn ? 'Starter 1' : 'Sơ cấp 1', focus: isEn ? 'Pinyin pronunciation, 150 basic words' : 'Phiên âm Pinyin, 150 từ vựng căn bản', completed: true },
      { code: 'HSK 2', name: isEn ? 'Starter 2' : 'Sơ cấp 2', focus: isEn ? 'Daily life interactions, 300 words' : 'Hội thoại đời sống thường nhật, 300 từ vựng', completed: false, current: true },
      { code: 'HSK 3', name: isEn ? 'Intermediate 1' : 'Trung cấp 1', focus: isEn ? 'Independent workplace conversations, 600 words' : 'Giao tiếp độc lập trong công việc, 600 từ vựng', completed: false },
      { code: 'HSK 4', name: isEn ? 'Intermediate 2' : 'Trung cấp 2', focus: isEn ? 'In-depth social topics, 1,200 words' : 'Thảo luận sâu nhiều chủ đề xã hội, 1,200 từ vựng', completed: false },
      { code: 'HSK 5', name: isEn ? 'Advanced 1' : 'Cao cấp 1', focus: isEn ? 'Newspapers, movies, presentations, 2,500 words' : 'Đọc báo chí, xem phim, thuyết trình, 2,500 từ vựng', completed: false },
      { code: 'HSK 6', name: isEn ? 'Mastery' : 'Thành thạo', focus: isEn ? 'Native-like fluency & writing, 5,000+ words' : 'Lưu loát toàn diện, viết văn bản chuyên sâu, 5,000+ từ', completed: false }
    ];
  }
  return [
    { code: 'A1', name: isEn ? 'Beginner' : 'Khởi động', focus: isEn ? 'Alphabet, greetings & basic vocabulary' : 'Bảng chữ cái, chào hỏi và từ vựng cơ bản', completed: true },
    { code: 'A2', name: isEn ? 'Elementary' : 'Cơ bản', focus: isEn ? 'Daily life, shopping & simple grammar' : 'Đời sống thường nhật, mua sắm, ngữ pháp đơn giản', completed: false, current: true },
    { code: 'B1', name: isEn ? 'Intermediate' : 'Trung cấp', focus: isEn ? 'Office communications, emails & documentation' : 'Giao tiếp văn phòng, viết email, đọc tài liệu', completed: false },
    { code: 'B2', name: isEn ? 'Upper Inter.' : 'Nâng cao', focus: isEn ? 'Technical discussions, project meetings' : 'Thảo luận kỹ thuật, thuyết trình dự án', completed: false },
    { code: 'C1', name: isEn ? 'Advanced' : 'Thành thạo', focus: isEn ? 'Strategic negotiations & academic writing' : 'Đàm phán chiến lược, viết luận văn chuyên môn', completed: false }
  ];
};

export const getRoadmapLevels = (langCode: string, lang: 'vi' | 'en' = 'vi') => {
  const builtIn = getBuiltInRoadmapLevels(langCode, lang);
  if (!managedLevels) return builtIn;
  return getLevelsForLanguage(langCode, lang).map(level => {
    const previous = builtIn.find(item => item.code === level.code);
    return { code: level.code, name: level.title, focus: level.desc,
      completed: previous?.completed ?? false, current: Boolean(previous && 'current' in previous && previous.current) };
  });
};
