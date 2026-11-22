import React, { useState, useEffect, useMemo } from 'react';
import { message } from 'antd';
import { useTranslation } from '../../i18n';
import { courseCategoriesApi, coursesApi } from '../../services/apiClient';
import type { Course, Language, StudyTarget } from '../../types';
import {
  getLevelsForLanguage,
  getDefaultLevelForLanguage,
  getLevelFrameworkName,
  DOMAINS,
  getDomainTitle,
  applyCourseCategories
} from '../../utils/languageLevels';
import { ExamRunnerModal } from '../quiz/ExamRunnerModal';
import {
  GlobalOutlined,
  CompassOutlined,
  CheckCircleOutlined,
  CloseOutlined,
  ExperimentOutlined,
  AimOutlined,
  MessageOutlined,
  CodeOutlined,
  DollarOutlined,
  RocketOutlined,
  ReadOutlined,
  AuditOutlined
} from '@ant-design/icons';

interface StudyTargetModalProps {
  isOpen: boolean;
  isAdmin?: boolean;
  onClose?: () => void;
  onSave: (target: StudyTarget) => Promise<boolean>;
  currentTarget?: StudyTarget | null;
  allowClose?: boolean;
}

const getDomainIcon = (code: string) => {
  switch (code) {
    case 'it':
      return <CodeOutlined className="text-blue-600 text-2xl mt-0.5" />;
    case 'business':
      return <DollarOutlined className="text-amber-600 text-2xl mt-0.5" />;
    case 'travel':
      return <RocketOutlined className="text-purple-600 text-2xl mt-0.5" />;
    case 'academic':
      return <ReadOutlined className="text-indigo-600 text-2xl mt-0.5" />;
    default:
      return <MessageOutlined className="text-emerald-600 text-2xl mt-0.5" />;
  }
};

export const StudyTargetModal: React.FC<StudyTargetModalProps> = ({
  isOpen,
  isAdmin = false,
  onClose,
  onSave,
  currentTarget,
  allowClose = true
}) => {
  const { t, language } = useTranslation();
  const languageLabel = (item: Language) => {
    const key = ({ en: 'english', ja: 'japanese', zh: 'chinese', cn: 'chinese', ko: 'korean' } as Record<string, string>)[item.code.toLowerCase()];
    return key ? t(`libraryPage.${key}`) : item.name;
  };
  const [languages, setLanguages] = useState<Language[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [categoryRevision, setCategoryRevision] = useState(0);
  const [languagesError, setLanguagesError] = useState(false);
  const [selectedLanguageCode, setSelectedLanguageCode] = useState<string>(
    currentTarget?.languageCode || 'en'
  );
  const [selectedLevel, setSelectedLevel] = useState<string>(currentTarget?.level || 'A1');
  const [selectedDomain, setSelectedDomain] = useState<string>(currentTarget?.domain || 'general');
  const [selectedDomains, setSelectedDomains] = useState<string[]>(currentTarget?.domains ?? [currentTarget?.domain || 'general']);

  // Quản lý trạng thái thi vượt cấp
  const [isExamModalOpen, setIsExamModalOpen] = useState(false);
  const [examTargetLevel, setExamTargetLevel] = useState('A2');
  const [examCourseId, setExamCourseId] = useState('');
  const [isPlacementTestMode, setIsPlacementTestMode] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    let isMounted = true;
    Promise.all([coursesApi.getLanguages(), coursesApi.getCourses(), courseCategoriesApi.list().catch(() => null)])
      .then(([availableLanguages, availableCourses, categories]) => {
        if (isMounted) {
          if (categories) {
            applyCourseCategories(categories);
            setCategoryRevision(value => value + 1);
          }
          setLanguages(availableLanguages);
          const eligible = availableCourses.filter((course) => course.hasMaterial);
          setCourses(eligible);
          if (!currentTarget && eligible.length > 0) {
            const first = eligible[0];
            setSelectedLanguageCode(availableLanguages.find((item) => item.id === first.languageId)?.code ?? '');
            setSelectedLevel(first.level);
            setSelectedDomain(first.domain ?? 'general');
            setSelectedDomains([first.domain ?? 'general']);
          }
        }
      })
      .catch(() => { if (isMounted) setLanguagesError(true); });
    return () => {
      isMounted = false;
    };
  }, [currentTarget, isOpen]);

  useEffect(() => {
    if (currentTarget) {
      setSelectedLanguageCode(currentTarget.languageCode);
      setSelectedLevel(currentTarget.level);
      setSelectedDomain(currentTarget.domain);
      setSelectedDomains(currentTarget.domains ?? [currentTarget.domain]);
    }
  }, [currentTarget]);

  // Compute available levels for currently selected language
  const availableLevels = useMemo(() => {
    const languageId = languages.find((item) => item.code === selectedLanguageCode)?.id;
    const validLevels = [...new Set(courses.filter((course) => course.languageId === languageId).map((course) => course.level))];
    const labels = getLevelsForLanguage(selectedLanguageCode, language);
    return validLevels.map((level) => labels.find((item) => item.code.toLowerCase() === level.toLowerCase()) ?? { code: level, title: level, desc: level });
  }, [selectedLanguageCode, language, languages, courses, categoryRevision]);

  const availableDomains = useMemo(() => {
    const languageId = languages.find((item) => item.code === selectedLanguageCode)?.id;
    const validDomains = [...new Set(courses.filter((course) => course.languageId === languageId && course.level.toLowerCase() === selectedLevel.toLowerCase()).map((course) => (course.domain ?? 'general').toLowerCase()))];
    return validDomains.map((code) => DOMAINS.find((domain) => domain.code === code) ?? { code, title: code, titleEn: code, desc: code, descEn: code });
  }, [selectedLanguageCode, selectedLevel, languages, courses, categoryRevision]);

  // Handle switching language: automatically adapt level framework
  const handleSelectLanguage = (code: string) => {
    setSelectedLanguageCode(code);
    const newLevels = getLevelsForLanguage(code, language);
    const languageId = languages.find((item) => item.code === code)?.id;
    const firstCourse = courses.find((course) => course.languageId === languageId);
    const nextLevel = newLevels.find((level) => level.code.toLowerCase() === firstCourse?.level.toLowerCase())?.code ?? getDefaultLevelForLanguage(code);
    setSelectedLevel(nextLevel);
    setSelectedDomains([]);
  };

  if (!isOpen) return null;

  const currentLangObj =
    languages.find((l) => l.code.toLowerCase() === selectedLanguageCode.toLowerCase()) ||
    languages[0];

  // Kiểm tra xem người dùng có đang cố gắng chọn level khác với level hiện tại khi cùng một ngôn ngữ
  const requiresPlacementTest = !isAdmin && (
    !currentTarget ||
    currentTarget.languageCode.toLowerCase() !== selectedLanguageCode.toLowerCase() ||
    currentTarget.level.toLowerCase() !== selectedLevel.toLowerCase());

  const isChangingToHigherLevel =
    !isAdmin && !!currentTarget &&
    currentTarget.languageCode.toLowerCase() === selectedLanguageCode.toLowerCase() &&
    currentTarget.level.toLowerCase() !== selectedLevel.toLowerCase();

  const handleLevelClick = (lvlCode: string) => {
    setSelectedDomains([]);
    setSelectedDomain('');
    // Nếu cùng ngôn ngữ và người dùng bấm chọn level khác
    if (
      currentTarget &&
      currentTarget.languageCode.toLowerCase() === selectedLanguageCode.toLowerCase() &&
      lvlCode.toLowerCase() !== currentTarget.level.toLowerCase()
    ) {
      setSelectedLevel(lvlCode);
      setExamTargetLevel(lvlCode);
      setIsPlacementTestMode(false);
    } else {
      setSelectedLevel(lvlCode);
    }
  };

  const openExam = (placement: boolean) => {
    if (selectedDomains.length === 0) {
      message.warning(t('studyTarget.selectDomainFirst'));
      return;
    }
    const languageId = languages.find((item) => item.code.toLowerCase() === selectedLanguageCode.toLowerCase())?.id;
    const matching = courses.filter((course) => course.languageId === languageId &&
      course.level.toLowerCase() === selectedLevel.toLowerCase() &&
      selectedDomains.some((domain) => domain.toLowerCase() === (course.domain ?? 'general').toLowerCase()));
    if (matching.length === 0) {
      message.error(t('studyTarget.noPlacementTest'));
      return;
    }
    setExamCourseId(matching[0].id);
    setExamTargetLevel(selectedLevel);
    setIsPlacementTestMode(placement);
    setIsExamModalOpen(true);
  };

  const handleStartLevelUpExam = () => { void openExam(false); };
  const handleStartPlacementTest = () => { void openExam(true); };

  const handleExamPass = async (passedLevel: string, placementProof: string) => {
    if (!currentLangObj || selectedDomains.length === 0) return;
    setSelectedLevel(passedLevel);
    // Tự động lưu target mới ngay khi thi đỗ
    const target: StudyTarget = {
      languageId: currentLangObj.id,
      languageCode: currentLangObj.code,
      languageName: currentLangObj.name,
      nativeName: currentLangObj.nativeName,
      flagEmoji: currentLangObj.flagEmoji || '🌐',
      level: passedLevel,
      domain: selectedDomains[0],
      domains: selectedDomains,
      placementProof
    };
    if (await onSave(target)) {
      setIsExamModalOpen(false);
      message.success(t('studyTarget.examPassed', { level: passedLevel }));
    }
  };

  const handleConfirm = async () => {
    if (!currentLangObj) {
      message.error(t(languagesError ? 'studyTarget.loadLanguagesError' : 'studyTarget.loadingLanguages'));
      return;
    }
    if (selectedDomains.length === 0 || !selectedDomains.every((domain) => availableDomains.some((item) => item.code === domain))) {
      message.error(t('studyTarget.selectValidDomain'));
      return;
    }
    if (requiresPlacementTest) {
      handleStartPlacementTest();
      return;
    }

    const target: StudyTarget = {
      languageId: currentLangObj.id,
      languageCode: currentLangObj.code,
      languageName: currentLangObj.name,
      nativeName: currentLangObj.nativeName,
      flagEmoji: currentLangObj.flagEmoji || '🌐',
      level: selectedLevel,
      domain: selectedDomains[0],
      domains: selectedDomains
    };
    if (await onSave(target)) message.success(t('common.success'));
  };

  if (!currentLangObj) {
    return <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60"><div className="bg-white rounded-xl p-6 text-stone-800">{t(languagesError ? 'studyTarget.loadLanguagesError' : 'studyTarget.loadingLanguages')}</div></div>;
  }

  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs overflow-y-auto">
      <div className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-2xl w-full p-6 my-8 space-y-6">
        <div className="flex items-center justify-between border-b-2 border-[#262626] pb-4">
          <div className="flex items-center gap-3">
            <span className="w-10 h-10 rounded-xl bg-amber-100 border border-amber-300 flex items-center justify-center text-xl text-amber-700">
              <AimOutlined />
            </span>
            <div>
              <h2 className="text-lg font-bold text-stone-900 tracking-tight">
                {t('studyTarget.modalTitle')}
              </h2>
              <p className="text-xs text-stone-600 font-medium">
                {t('studyTarget.modalSubtitle')}
              </p>
            </div>
          </div>
          {allowClose && onClose && (
            <button
              onClick={onClose}
              className="p-1.5 rounded-lg border border-stone-300 hover:bg-stone-200 text-stone-600 transition cursor-pointer"
            >
              <CloseOutlined />
            </button>
          )}
        </div>

        <div className="space-y-5 max-h-[65vh] overflow-y-auto pr-1">
          {/* Select Language */}
          <div>
            <label className="flex items-center gap-2 text-xs font-bold text-stone-800 uppercase tracking-wider mb-2.5">
              <GlobalOutlined className="text-amber-600" />
              <span>{t('studyTarget.selectLanguage')}</span>
            </label>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
              {languages.filter((lang) => courses.some((course) => course.languageId === lang.id)).map((lang) => {
                const isSelected =
                  lang.code.toLowerCase() === selectedLanguageCode.toLowerCase();
                return (
                  <button
                    key={lang.code}
                    type="button"
                    onClick={() => handleSelectLanguage(lang.code)}
                    className={`p-3 rounded-xl border-2 text-left transition flex items-center gap-2.5 cursor-pointer ${
                      isSelected
                        ? 'border-[#262626] bg-amber-50 shadow-xs ring-2 ring-amber-300/60'
                        : 'border-stone-300 bg-white hover:border-stone-400'
                    }`}
                  >
                    <span className="text-2xl">{lang.flagEmoji || '🌐'}</span>
                    <div className="truncate">
                      <div className="text-xs font-bold text-stone-900 truncate">
                        {languageLabel(lang)}
                      </div>
                      <div className="text-[10px] text-stone-500 font-mono truncate">
                        {lang.nativeName || lang.code.toUpperCase()}
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Select Level (Dynamic framework according to language) */}
          <div>
            <div className="flex items-center justify-between mb-2.5">
              <label className="flex items-center gap-2 text-xs font-bold text-stone-800 uppercase tracking-wider">
                <CompassOutlined className="text-indigo-600" />
                <span>{t('studyTarget.selectLevel')}</span>
              </label>
              <span className="text-[11px] font-mono font-bold px-2 py-0.5 rounded bg-indigo-50 text-indigo-700 border border-indigo-200">
                {language === 'vi' ? 'Chuẩn: ' : 'Framework: '} {getLevelFrameworkName(selectedLanguageCode, language)}
              </span>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-5 gap-2">
              {availableLevels.map((lvl) => {
                const isSelected = selectedLevel.toLowerCase() === lvl.code.toLowerCase();
                const isCurrentLearnerLevel =
                  currentTarget &&
                  currentTarget.languageCode.toLowerCase() === selectedLanguageCode.toLowerCase() &&
                  currentTarget.level.toLowerCase() === lvl.code.toLowerCase();

                return (
                  <button
                    key={lvl.code}
                    type="button"
                    onClick={() => handleLevelClick(lvl.code)}
                    className={`p-2.5 rounded-xl border-2 text-center transition cursor-pointer flex flex-col justify-between relative ${
                      isSelected
                        ? 'border-[#262626] bg-indigo-50 text-indigo-900 shadow-xs ring-2 ring-indigo-300/60'
                        : 'border-stone-300 bg-white hover:border-stone-400 text-stone-700'
                    }`}
                    title={lvl.desc}
                  >
                    <div className="text-sm font-extrabold font-mono flex items-center justify-center gap-1">
                      <span>{lvl.code}</span>
                      {isCurrentLearnerLevel && (
                        <span className="text-[9px] px-1 py-0.2 bg-emerald-100 text-emerald-800 rounded font-sans">
                          {t('studyTarget.currentLevel')}
                        </span>
                      )}
                    </div>
                    <div className="text-[10px] font-bold truncate mt-0.5">
                      {lvl.title}
                    </div>
                  </button>
                );
              })}
            </div>

            {/* CẢNH BÁO: BẮT BUỘC PHẢI THI ĐỖ VƯỢT CẤP MỚI ĐƯỢC CHUYỂN LEVEL */}
            {isChangingToHigherLevel && (
              <div className="mt-3 p-3.5 bg-amber-50 border-2 border-amber-300 rounded-xl space-y-2">
                <div className="flex items-start gap-2.5">
                  <AuditOutlined className="text-amber-700 text-base mt-0.5" />
                  <div className="text-xs">
                    <div className="font-bold text-amber-900">
                      {t('studyTarget.levelChangeTitle', { level: selectedLevel })}
                    </div>
                    <p className="text-stone-600 mt-0.5">
                      {t('studyTarget.levelChangeHint', { current: currentTarget.level, level: selectedLevel })}
                    </p>
                  </div>
                </div>

                <div className="flex justify-end pt-1">
                  <button
                    type="button"
                    onClick={handleStartLevelUpExam}
                    className="px-4 py-1.5 bg-amber-400 hover:bg-amber-500 text-stone-900 text-xs font-black rounded-lg notebook-btn flex items-center gap-1.5 cursor-pointer shadow-2xs"
                  >
                    <AuditOutlined />
                    <span>{t('studyTarget.takeLevelTest', { level: selectedLevel })}</span>
                  </button>
                </div>
              </div>
            )}

            {/* Nút làm bài kiểm tra đầu vào khi chưa có mục tiêu (Học viên mới) */}
            {!isAdmin && !currentTarget && (
              <div className="mt-2.5 flex items-center justify-between p-2.5 bg-stone-100/80 border border-stone-200 rounded-lg text-xs">
                <span className="text-stone-600">
                  {t('studyTarget.unsureLevel')}
                </span>
                <button
                  type="button"
                  onClick={handleStartPlacementTest}
                  className="px-2.5 py-1 bg-white hover:bg-stone-50 border border-stone-300 text-stone-800 rounded font-bold transition flex items-center gap-1 cursor-pointer"
                >
                  <AuditOutlined className="text-indigo-600" />
                  <span>{t('studyTarget.takePlacementTest')}</span>
                </button>
              </div>
            )}
          </div>

          {/* Select Domain / Field */}
          <div>
            <label className="flex items-center gap-2 text-xs font-bold text-stone-800 uppercase tracking-wider mb-2.5">
              <ExperimentOutlined className="text-emerald-600" />
              <span>{t('studyTarget.selectDomain')}</span>
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
              {availableDomains.map((domain) => {
                const isSelected = selectedDomains.includes(domain.code);
                return (
                  <button
                    key={domain.code}
                    type="button"
                    onClick={() => {
                      const next = isSelected ? selectedDomains.filter((code) => code !== domain.code) : [...selectedDomains, domain.code];
                      setSelectedDomains(next);
                      setSelectedDomain(next[0] ?? '');
                    }}
                    className={`p-3 rounded-xl border-2 text-left transition flex items-start gap-3 cursor-pointer ${
                      isSelected
                        ? 'border-[#262626] bg-emerald-50 shadow-xs ring-2 ring-emerald-300/60'
                        : 'border-stone-300 bg-white hover:border-stone-400'
                    }`}
                  >
                    <span className="flex items-center justify-center">{getDomainIcon(domain.code)}</span>
                    <div className="flex-1">
                      <div className="text-xs font-bold text-stone-900">
                        {getDomainTitle(domain.code, language)}
                      </div>
                      <div className="text-[11px] text-stone-500 line-clamp-2 mt-0.5">
                        {language === 'vi' ? domain.desc : domain.descEn}
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>
        </div>

        {/* Footer Confirmation Bar */}
        <div className="border-t-2 border-[#262626] pt-4 flex flex-wrap items-center justify-between gap-3">
          <div className="text-xs text-stone-600 font-mono flex items-center gap-1.5">
            <span>{currentLangObj.flagEmoji || '🌐'}</span>
            <span className="font-bold text-stone-800">{languageLabel(currentLangObj)}</span>
            <span>•</span>
            <span className="font-bold text-indigo-700">{selectedLevel}</span>
            <span>•</span>
            <span className="font-bold text-emerald-700 uppercase">{selectedDomain}</span>
          </div>

          <button
            type="button"
            onClick={handleConfirm}
            className={`px-5 py-2.5 rounded-xl text-xs font-bold notebook-btn flex items-center gap-2 transition cursor-pointer ${
              isChangingToHigherLevel
                ? 'bg-amber-400 hover:bg-amber-500 text-stone-900 font-black'
                : 'bg-stone-900 hover:bg-stone-800 text-white'
            }`}
          >
            {isChangingToHigherLevel ? <AuditOutlined /> : <CheckCircleOutlined />}
            <span>
              {isChangingToHigherLevel
                ? t('studyTarget.takeLevelTest', { level: selectedLevel })
                : t('studyTarget.confirmAndStart')}
            </span>
          </button>
        </div>
      </div>

      {/* MODAL PHÒNG THI VƯỢT CẤP & ĐÁNH GIÁ NĂNG LỰC */}
      <ExamRunnerModal
        isOpen={isExamModalOpen}
        onClose={() => setIsExamModalOpen(false)}
        languageCode={selectedLanguageCode}
        targetLevel={examTargetLevel}
        courseId={examCourseId}
        isPlacementTest={isPlacementTestMode}
        onPassLevel={handleExamPass}
      />
    </div>
  );
};
