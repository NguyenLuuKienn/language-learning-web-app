import React, { useState, useEffect, useRef } from 'react';
import { BrowserRouter, Navigate, useNavigate, useLocation } from 'react-router-dom';
import { message, ConfigProvider, App as AntdApp } from 'antd';
import { LanguageProvider, useTranslation } from './i18n';
import { AuthProvider, useAuth } from './features/auth';
import { AuthModal } from './features/auth/AuthModal';
import { SettingsModal } from './features/settings/SettingsModal';
import { CsvWizardModal } from './features/csv/CsvWizardModal';
import { StudyTargetModal } from './features/onboarding/StudyTargetModal';
import { HomeView } from './features/home/HomeView';
const CourseCatalogView = React.lazy(() => import('./features/courses/CourseCatalogView').then((module) => ({ default: module.CourseCatalogView })));
const TopicWritingView = React.lazy(() => import('./features/practice/TopicWritingView').then((module) => ({ default: module.TopicWritingView })));
const SpeakingReadingView = React.lazy(() => import('./features/practice/SpeakingReadingView').then((module) => ({ default: module.SpeakingReadingView })));
import { MyDecksView } from './features/decks/MyDecksView';
import { SrsReviewView } from './features/reviews/SrsReviewView';
import { VocabularyHubView } from './features/vocabulary/VocabularyHubView';
import { QuizPlayerView } from './features/quiz/QuizPlayerView';
import { QuizHubView } from './features/quiz/QuizHubView';
import { StatisticsView } from './features/statistics/StatisticsView';
import { PublicDecksView } from './features/sharing/PublicDecksView';
import { SyncView } from './features/sync/SyncView';
import { clearOfflineStudyData } from './offline/db';
const AdminView = React.lazy(() => import('./features/admin/AdminView').then((module) => ({ default: module.AdminView })));
import { LandingPage } from './features/landing/LandingPage';
import { LearningRoadmapView } from './features/roadmap/LearningRoadmapView';
const ListeningView = React.lazy(() => import('./features/practice/ListeningView').then((module) => ({ default: module.ListeningView })));
const AiConversationView = React.lazy(() => import('./features/practice/AiConversationView').then((module) => ({ default: module.AiConversationView })));
const SpecialPracticeView = React.lazy(() => import('./features/specialPractice/SpecialPracticeView').then((module) => ({ default: module.SpecialPracticeView })));
const GrammarView = React.lazy(() => import('./features/practice/GrammarView').then((module) => ({ default: module.GrammarView })));
const ReadingView = React.lazy(() => import('./features/practice/ReadingView').then((module) => ({ default: module.ReadingView })));
const LessonPlayerView = React.lazy(() => import('./features/journey/LessonPlayerView').then((module) => ({ default: module.LessonPlayerView })));
const CharacterLabView = React.lazy(() => import('./features/character/CharacterLabView').then((module) => ({ default: module.CharacterLabView })));
const LibraryView = React.lazy(() => import('./features/library/LibraryView').then((module) => ({ default: module.LibraryView })));
import { LeaderboardView } from './features/gamification/LeaderboardView';
import { QuickDictionaryModal } from './components/dictionary/QuickDictionaryModal';
import { NotebookWidget } from './components/notebook/NotebookWidget';
import { StudyGoalRequired } from './components/ui/StudyGoalRequired';
import { isAdminUser, type StudyTarget, type UserProfile } from './types';
import { coursesApi, studyTargetApi, getErrorMessage } from './services/apiClient';
import { curriculumStore, type CurriculumCourse } from './services/curriculumStore';
import {
  HomeOutlined,
  BookOutlined,
  EditOutlined,
  SoundOutlined,
  CopyOutlined,
  BarChartOutlined,
  GlobalOutlined,
  CloudSyncOutlined,
  SafetyCertificateOutlined,
  SettingOutlined,
  LogoutOutlined,
  FireOutlined,
  UserOutlined,
  CompassOutlined,
  CustomerServiceOutlined,
  MessageOutlined,
  ReadOutlined,
  FileTextOutlined,
  ApartmentOutlined,
  CheckSquareOutlined,
  SyncOutlined,
  HighlightOutlined,
  TrophyOutlined
} from '@ant-design/icons';

type ActiveView =
  | 'landing'
  | 'home'
  | 'roadmap'
  | 'courses'
  | 'curriculum'
  | 'learn'
  | 'listening'
  | 'speaking'
  | 'reading'
  | 'writing'
  | 'grammar'
  | 'characterLab'
  | 'library'
  | 'leaderboard'
  | 'aiChat'
  | 'certificatePractice'
  | 'interviewPractice'
  | 'decks'
  | 'srsReview'
  | 'quiz'
  | 'statistics'
  | 'sharing'
  | 'sync'
  | 'admin';

const VIEW_TO_PATH: Record<ActiveView, string> = {
  landing: '/',
  home: '/home',
  roadmap: '/roadmap',
  courses: '/courses',
  curriculum: '/curriculum',
  learn: '/learn',
  listening: '/listening',
  speaking: '/speaking',
  reading: '/reading',
  writing: '/writing',
  grammar: '/grammar',
  characterLab: '/character-lab',
  library: '/library',
  leaderboard: '/leaderboard',
  aiChat: '/ai-chat',
  certificatePractice: '/certificate-practice',
  interviewPractice: '/interview-practice',
  decks: '/decks',
  srsReview: '/review',
  quiz: '/quiz',
  statistics: '/stats',
  sharing: '/community',
  sync: '/sync',
  admin: '/admin'
};

const PATH_TO_VIEW: Record<string, ActiveView> = {
  '/': 'landing',
  '/home': 'home',
  '/roadmap': 'roadmap',
  '/courses': 'courses',
  '/vocabulary': 'courses',
  '/curriculum': 'curriculum',
  '/listening': 'listening',
  '/practice-listening': 'listening',
  '/speaking': 'speaking',
  '/practice-speaking': 'speaking',
  '/reading': 'reading',
  '/practice-reading': 'reading',
  '/writing': 'writing',
  '/practice-writing': 'writing',
  '/grammar': 'grammar',
  '/practice-grammar': 'grammar',
  '/character-lab': 'characterLab',
  '/alphabet': 'characterLab',
  '/library': 'library',
  '/leaderboard': 'leaderboard',
  '/ranking': 'leaderboard',
  '/ai-chat': 'aiChat',
  '/certificate-practice': 'certificatePractice',
  '/interview-practice': 'interviewPractice',
  '/decks': 'decks',
  '/review': 'srsReview',
  '/srs-review': 'srsReview',
  '/quiz': 'quiz',
  '/stats': 'statistics',
  '/statistics': 'statistics',
  '/community': 'sharing',
  '/sharing': 'sharing',
  '/sync': 'sync',
  '/admin': 'admin'
};

const MainShell: React.FC = () => {
  const { t, language, toggleLanguage } = useTranslation();
  const { user, isAuthenticated, isLoading: isAuthLoading, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  // Đồng bộ view dựa trên URL pathname
  const currentPath = location.pathname.toLowerCase() || '/';
  const currentView: ActiveView = currentPath.startsWith('/learn/') ? 'learn' :
    currentPath === '/courses' && new URLSearchParams(location.search).has('courseId')
    ? 'curriculum' : PATH_TO_VIEW[currentPath] || 'landing';
  const requiresMaterials = ['roadmap', 'listening', 'speaking', 'reading', 'writing', 'grammar', 'aiChat', 'library'].includes(currentView);
  const requiresStudyGoal = ['roadmap', 'learn', 'courses', 'curriculum', 'listening', 'speaking', 'reading',
    'writing', 'grammar', 'characterLab', 'library', 'aiChat', 'certificatePractice', 'interviewPractice',
    'decks', 'srsReview', 'quiz', 'sharing'].includes(currentView);

  const [activeQuizLesson, setActiveQuizLesson] = useState<{ id: string; title: string } | null>(
    null
  );
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
  const [isSettingsModalOpen, setIsSettingsModalOpen] = useState(false);
  const [csvDeckId, setCsvDeckId] = useState<string | null>(null);
  const [selectedStudyDeckId, setSelectedStudyDeckId] = useState<string | undefined>(undefined);
  const [isDictionaryOpen, setIsDictionaryOpen] = useState(false);
  const [materialsReady, setMaterialsReady] = useState(false);
  const [materialsError, setMaterialsError] = useState(false);
  const [materialsReloadKey, setMaterialsReloadKey] = useState(0);
  const [materialsVersion, setMaterialsVersion] = useState(0);
  const [dictionaryLookupWord, setDictionaryLookupWord] = useState('');
  const [studyTarget, setStudyTarget] = useState<StudyTarget | null>(null);
  const [restoredTargetUserId, setRestoredTargetUserId] = useState<string | null>(null);
  const materialCache = useRef<{ key: string; loadedAt: number } | null>(null);

  useEffect(() => {
    const invalidate = () => { materialCache.current = null; };
    window.addEventListener('curriculum:updated', invalidate);
    return () => window.removeEventListener('curriculum:updated', invalidate);
  }, []);

  useEffect(() => {
    if (restoredTargetUserId !== (user?.id ?? 'guest')) return;
    if (!requiresMaterials) return;
    setMaterialsError(false);
    const cacheKey = `${user?.id ?? 'guest'}:${JSON.stringify(studyTarget)}`;
    if (materialCache.current?.key === cacheKey && Date.now() - materialCache.current.loadedAt < 60_000) {
      setMaterialsReady(true);
      return;
    }
    let active = true;
    const controller = new AbortController();
    setMaterialsReady(false);
    setMaterialsError(false);
    if (!studyTarget) {
      materialCache.current = null;
      curriculumStore.replaceFromServer([]);
      setMaterialsReady(true);
      setMaterialsVersion((version) => version + 1);
      return;
    }
    coursesApi.getCourses(studyTarget.languageId, studyTarget.level, undefined, controller.signal)
      .then((courses) => {
        const relevant = courses.filter((course) =>
          course.hasMaterial &&
          (studyTarget.domains?.length ? studyTarget.domains : [studyTarget.domain])
            .some((domain) => domain.toLowerCase() === (course.domain ?? 'general').toLowerCase())
        );
        return Promise.allSettled(relevant.map((course) => coursesApi.getCourseMaterial(course.id, controller.signal)));
      })
      .then((results) => {
        if (!active) return;
        if (results.some((result) => result.status === 'rejected')) {
          materialCache.current = null;
          setMaterialsError(true);
          return;
        }
        const materials = results.flatMap((result) => result.status === 'fulfilled' ? [result.value] : []);
        const valid = materials.filter((item): item is CurriculumCourse =>
          typeof item === 'object' && item !== null && 'units' in item && Array.isArray(item.units)
        );
        curriculumStore.replaceFromServer(valid);
        materialCache.current = { key: cacheKey, loadedAt: Date.now() };
      })
      .catch(() => {
        if (active) {
          materialCache.current = null;
          setMaterialsError(true);
        }
      })
      .finally(() => {
        if (active) {
          setMaterialsReady(true);
          setMaterialsVersion((version) => version + 1);
        }
      });
    return () => { active = false; controller.abort(); };
  }, [studyTarget, restoredTargetUserId, user?.id, requiresMaterials, materialsReloadKey]);

  useEffect(() => {
    let active = true;
    if (!user?.id) {
      setStudyTarget(null);
      setRestoredTargetUserId('guest');
      return () => { active = false; };
    }
    const userId = user.id;
    studyTargetApi.get()
      .then(async (saved) => {
        if (saved) return saved;
        const legacy = localStorage.getItem(`study_target_${userId}`);
        if (!legacy) return null;
        const migrated = await studyTargetApi.save(JSON.parse(legacy) as StudyTarget);
        localStorage.removeItem(`study_target_${userId}`);
        localStorage.removeItem(`onboarding_completed_${userId}`);
        return migrated;
      })
      .then((saved) => { if (active) setStudyTarget(saved); })
      .catch((error) => {
        if (active) { setStudyTarget(null); message.error(getErrorMessage(error, t('common.error'))); }
      })
      .finally(() => { if (active) setRestoredTargetUserId(userId); });
    return () => { active = false; };
  }, [user?.id]);

  const [isTargetModalOpen, setIsTargetModalOpen] = useState<boolean>(false);

  useEffect(() => {
    if ((currentView === 'courses' || currentView === 'home') && user?.id && restoredTargetUserId === user.id && !studyTarget && !isAdminUser(user)) {
      setIsTargetModalOpen(true);
    }
  }, [currentView, user, restoredTargetUserId, studyTarget]);

  // Điều hướng có lưu lịch sử trình duyệt để Back/Forward hoạt động chuẩn
  const handleNavigate = (viewOrPath: ActiveView | string) => {
    if (viewOrPath in VIEW_TO_PATH) {
      navigate(VIEW_TO_PATH[viewOrPath as ActiveView]);
    } else if (viewOrPath.startsWith('/')) {
      navigate(viewOrPath);
    } else {
      navigate(`/${viewOrPath}`);
    }
  };

  const handleBack = () => {
    if (window.history.length > 1) {
      navigate(-1);
    } else {
      navigate('/home');
    }
  };

  useEffect(() => {
    if (!isAuthLoading && currentView === 'admin' && (!isAuthenticated || !isAdminUser(user))) {
      navigate('/');
    }
  }, [currentView, isAuthLoading, isAuthenticated, user, navigate]);

  const handleLoginSuccess = (loggedUser: UserProfile) => {
    if (isAdminUser(loggedUser)) {
      handleNavigate('admin');
    } else {
      handleNavigate('home');
    }
  };

  const handleSaveStudyTarget = async (target: StudyTarget) => {
    try {
      const saved = await studyTargetApi.save(target);
      const changed = studyTarget?.languageId !== saved.languageId || studyTarget?.level !== saved.level ||
          JSON.stringify(studyTarget?.domains ?? []) !== JSON.stringify(saved.domains ?? []);
      setStudyTarget(saved);
      setIsTargetModalOpen(false);
      if (changed) {
        try { await clearOfflineStudyData(); }
        catch { message.warning(language === 'vi' ? 'Đã lưu mục tiêu học, nhưng chưa xóa được dữ liệu offline cũ.' : 'Study goal saved, but old offline data could not be cleared.'); }
      }
      return true;
    } catch (error) {
      message.error(getErrorMessage(error, t('common.error')));
      return false;
    }
  };


  const handleStartQuiz = (lessonId: string, lessonTitle: string) => {
    setActiveQuizLesson({ id: lessonId, title: lessonTitle });
    handleNavigate('quiz');
  };

  const handleStudyDeck = (deckId?: string) => {
    setSelectedStudyDeckId(deckId);
    handleNavigate('srsReview');
  };

  const handleLogout = async () => {
    await logout();
    try { await clearOfflineStudyData(); }
    catch { message.warning(language === 'vi' ? 'Không xóa được dữ liệu offline trên thiết bị.' : 'Offline data could not be cleared from this device.'); }
    handleNavigate('landing');
    message.success(t('auth.logoutSuccess'));
  };

  if (currentView !== 'landing' && isAuthLoading) {
    return <div className="min-h-screen flex items-center justify-center" role="status">{t('common.loading')}</div>;
  }

  if (currentView !== 'landing' && !isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  // If on standalone Landing Page, render full width without sidebar
  if (currentView === 'landing') {
    return (
      <div className="min-h-screen bg-[#FAF6EE]">
        <LandingPage
          onEnterApp={(target) => isAuthenticated ? handleNavigate(target || 'home') : setIsAuthModalOpen(true)}
          onOpenAuth={() => setIsAuthModalOpen(true)}
          onOpenTargetModal={() => isAuthenticated ? setIsTargetModalOpen(true) : setIsAuthModalOpen(true)}
        />

        <AuthModal
          isOpen={isAuthModalOpen}
          onClose={() => setIsAuthModalOpen(false)}
          onLoginSuccess={handleLoginSuccess}
        />

        <StudyTargetModal
          isOpen={isTargetModalOpen}
          isAdmin={isAdminUser(user)}
          currentTarget={studyTarget}
          allowClose={true}
          onClose={() => setIsTargetModalOpen(false)}
          onSave={handleSaveStudyTarget}
        />
        <NotebookWidget
          isAuthenticated={isAuthenticated}
          onRequestLogin={() => setIsAuthModalOpen(true)}
          languageCode={studyTarget?.languageCode}
          sourcePath={currentPath}
          onDictionary={(word) => { setDictionaryLookupWord(word); setIsDictionaryOpen(true); }}
        />
        <QuickDictionaryModal
          isOpen={isDictionaryOpen}
          onClose={() => setIsDictionaryOpen(false)}
          initialWord={dictionaryLookupWord}
          target={studyTarget}
        />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex bg-[#FAF6EE] text-[#1F1F1F] font-sans antialiased">
      <aside className="w-[285px] bg-[#FAF6EE] border-r-2 border-[#262626] flex flex-col justify-between shrink-0 select-none">
        <div>
          <div className="p-3 border-b-2 border-[#262626] flex items-center justify-between gap-2">
            <button
              onClick={() => handleNavigate('landing')}
              className="flex items-center gap-2 cursor-pointer text-left shrink-0"
              title={language === 'vi' ? 'Về Trang chủ giới thiệu' : 'Back to Landing Page'}
            >
              <span className="bg-[#E11D48] text-white text-[11px] font-mono font-bold px-1.5 py-0.5 rounded border border-[#9F1239]">
                SRS
              </span>
              <div className="leading-tight">
                <span className="font-bold text-sm block tracking-tight text-stone-900">
                  LinguaFlow
                </span>
                <span className="text-[9px] uppercase font-bold text-stone-500 block -mt-0.5 tracking-wider font-mono">
                  LEARNING
                </span>
              </div>
            </button>

            <div className="flex items-center gap-1.5 shrink-0">
              <div
                className="flex items-center gap-1 text-xs font-bold text-amber-600 bg-amber-50 px-2 py-0.5 rounded border border-amber-300"
                title={`${user?.currentStreak || 0} ${t('stats.days')}`}
              >
                <FireOutlined className="text-sm text-amber-500" />
                <span>{user?.currentStreak || 0}</span>
              </div>

              <button
                onClick={toggleLanguage}
                className="px-2 py-0.5 text-xs font-mono font-bold rounded border-notebook bg-white hover:bg-stone-100 transition cursor-pointer"
                title="Switch Language (EN / VI)"
              >
                {language.toUpperCase()}
              </button>
            </div>
          </div>

          {/* Khối Mục Tiêu Ngôn Ngữ Duy Nhất (Không Trùng Lặp) */}
          {studyTarget ? (
            <div className="px-3 pt-3 pb-1">
              <div className="p-2.5 rounded-xl bg-white border-notebook shadow-2xs">
                <div className="flex items-center justify-between mb-1.5">
                  <span className="text-[10px] font-bold text-stone-500 uppercase tracking-wider">
                    {language === 'vi' ? 'Ngôn ngữ đang học' : 'Learning Language'}
                  </span>
                  <button
                    onClick={() => setIsTargetModalOpen(true)}
                    className="text-[10px] font-bold text-amber-800 hover:text-amber-950 hover:underline cursor-pointer flex items-center gap-0.5"
                    title={language === 'vi' ? 'Chuyển đổi ngôn ngữ hoặc nâng cấp trình độ' : 'Change language or level'}
                  >
                    <span>{language === 'vi' ? 'Đổi' : 'Change'}</span>
                    <CompassOutlined className="text-[9px]" />
                  </button>
                </div>
                <div className="flex items-center gap-2.5">
                  <span className="text-2xl">{studyTarget.flagEmoji || '🌐'}</span>
                  <div className="flex-1 min-w-0">
                    <span className="text-xs font-bold text-stone-900 block truncate">
                      {studyTarget.languageName}
                    </span>
                    <span className="text-[10px] text-stone-600 font-mono block">
                      {studyTarget.level} • {studyTarget.domain.toUpperCase()}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="px-3 pt-3 pb-1">
              <button
                onClick={() => setIsTargetModalOpen(true)}
                className="w-full py-2 px-3 rounded-xl border border-dashed border-stone-300 bg-white/70 hover:bg-white text-xs font-bold text-stone-700 transition flex items-center justify-center gap-2 cursor-pointer"
              >
                <CompassOutlined className="text-amber-600" />
                <span>{language === 'vi' ? 'Chọn ngôn ngữ & Mục tiêu' : 'Set Study Target'}</span>
              </button>
            </div>
          )}

          <nav className="p-3 space-y-4">
            {/* NHÓM 1: TỔNG QUAN */}
            <div>
              <span className="px-3 text-[10px] font-bold text-stone-500 uppercase tracking-wider block mb-1">
                {t('nav.groupOverview')}
              </span>
              <div className="space-y-0.5">
                <button
                  onClick={() => handleNavigate('home')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'home'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <HomeOutlined />
                  <span>{t('nav.home')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('roadmap')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'roadmap'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <ApartmentOutlined />
                  <span>{t('nav.roadmap')}</span>
                </button>

              </div>
            </div>

            {/* NHÓM 2: RÈN LUYỆN KỸ NĂNG */}
            <div>
              <span className="px-3 text-[10px] font-bold text-stone-500 uppercase tracking-wider block mb-1">
                {t('nav.groupSkills')}
              </span>
              <div className="space-y-0.5">
                <button
                  onClick={() => handleNavigate('listening')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'listening'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <CustomerServiceOutlined />
                  <span>{t('nav.listening')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('speaking')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'speaking'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <SoundOutlined />
                  <span>{t('nav.speaking')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('reading')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'reading'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <ReadOutlined />
                  <span>{t('nav.reading')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('writing')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'writing'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <EditOutlined />
                  <span>{t('nav.writing')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('grammar')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'grammar'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <FileTextOutlined />
                  <span>{t('nav.grammar')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('characterLab')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'characterLab'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <HighlightOutlined className="text-rose-600" />
                  <span>
                    {studyTarget?.languageCode === 'en'
                      ? (language === 'vi' ? 'Bảng phát âm IPA' : 'IPA Chart')
                      : (language === 'vi' ? 'Phòng luyện chữ' : 'Character Lab')}
                  </span>
                </button>

                <button
                  onClick={() => handleNavigate('library')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'library'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <ReadOutlined className="text-emerald-600" />
                  <span>{language === 'vi' ? 'Thư viện & Podcast' : 'Library & Media'}</span>
                </button>

                <button
                  onClick={() => handleNavigate('aiChat')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'aiChat'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <MessageOutlined />
                  <span>{t('nav.aiChat')}</span>
                </button>
                <button onClick={() => handleNavigate('certificatePractice')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-left ${currentView === 'certificatePractice' ? 'bg-white border-notebook font-bold' : 'text-stone-600 hover:bg-stone-200/50'}`}>
                  <SafetyCertificateOutlined /><span>{language === 'vi' ? 'Luyện thi chứng chỉ' : 'Certificate practice'}</span>
                </button>
                <button onClick={() => handleNavigate('interviewPractice')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold text-left ${currentView === 'interviewPractice' ? 'bg-white border-notebook font-bold' : 'text-stone-600 hover:bg-stone-200/50'}`}>
                  <MessageOutlined /><span>{language === 'vi' ? 'Luyện phỏng vấn' : 'Interview practice'}</span>
                </button>
              </div>
            </div>

            {/* NHÓM 3: TỪ VỰNG & ÔN TẬP */}
            <div>
              <span className="px-3 text-[10px] font-bold text-stone-500 uppercase tracking-wider block mb-1">
                {t('nav.groupVocab')}
              </span>
              <div className="space-y-0.5">
                <button
                  onClick={() => handleNavigate('courses')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'courses'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <BookOutlined />
                  <span>{t('nav.courses')}</span>
                </button>
                <button
                  onClick={() => handleNavigate('srsReview')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'srsReview'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <SyncOutlined className="text-emerald-600" />
                  <span>{language === 'vi' ? 'Ôn tập' : 'Review'}</span>
                </button>
                <button
                  onClick={() => handleNavigate('decks')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'decks'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <CopyOutlined />
                  <span>{t('nav.myDecks')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('sharing')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'sharing'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <GlobalOutlined />
                  <span>{t('nav.publicDecks')}</span>
                </button>

                <button
                  onClick={() => {
                    setActiveQuizLesson(null);
                    handleNavigate('quiz');
                  }}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'quiz'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <CheckSquareOutlined />
                  <span>{t('nav.quiz')}</span>
                </button>
              </div>
            </div>

            {/* NHÓM 4: TIẾN ĐỘ & HỆ THỐNG */}
            <div>
              <span className="px-3 text-[10px] font-bold text-stone-500 uppercase tracking-wider block mb-1">
                {t('nav.groupSystem')}
              </span>
              <div className="space-y-0.5">
                <button
                  onClick={() => handleNavigate('statistics')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'statistics'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <BarChartOutlined />
                  <span>{t('nav.statistics')}</span>
                </button>

                <button
                  onClick={() => handleNavigate('leaderboard')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'leaderboard'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <TrophyOutlined className="text-amber-500" />
                  <span>{language === 'vi' ? 'Bảng xếp hạng' : 'Leaderboard'}</span>
                </button>

                <button
                  onClick={() => handleNavigate('sync')}
                  className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                    currentView === 'sync'
                      ? 'bg-white border-notebook shadow-2xs text-stone-900 font-bold'
                      : 'text-stone-600 hover:bg-stone-200/50'
                  }`}
                >
                  <CloudSyncOutlined />
                  <span>{t('nav.sync')}</span>
                </button>

                {isAuthenticated && isAdminUser(user) && (
                  <button
                    onClick={() => handleNavigate('admin')}
                    className={`w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-semibold transition text-left cursor-pointer ${
                      currentView === 'admin'
                        ? 'bg-rose-50 border-notebook shadow-2xs text-rose-900 font-bold'
                        : 'text-stone-500 hover:bg-stone-200/50'
                    }`}
                  >
                    <SafetyCertificateOutlined className="text-rose-600" />
                    <span>{t('nav.admin')}</span>
                  </button>
                )}
              </div>
            </div>
          </nav>
        </div>

        <div className="p-3 border-t-2 border-[#262626] space-y-2">
          {isAuthenticated && user ? (
            <div className="p-2 bg-white border-notebook rounded-lg flex items-center justify-between">
              <div className="flex items-center gap-2 truncate">
                <div className="w-7 h-7 rounded-full bg-stone-900 text-white flex items-center justify-center text-xs font-bold">
                  {user.displayName.charAt(0).toUpperCase()}
                </div>
                <div className="truncate">
                  <span className="text-xs font-bold text-stone-900 block truncate">
                    {user.displayName}
                  </span>
                  <span className="text-[10px] text-stone-500 font-mono block">
                    {user.role}
                  </span>
                </div>
              </div>
              <button
                onClick={() => setIsSettingsModalOpen(true)}
                className="text-stone-500 hover:text-stone-900 p-1"
                title={t('nav.settings')}
              >
                <SettingOutlined />
              </button>
            </div>
          ) : (
            <button
              onClick={() => setIsAuthModalOpen(true)}
              className="w-full py-2 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-xs font-bold notebook-btn flex items-center justify-center gap-2 transition cursor-pointer"
            >
              <UserOutlined />
              <span>{t('nav.login')}</span>
            </button>
          )}

          {isAuthenticated && (
            <button
              onClick={handleLogout}
              className="w-full py-1.5 rounded-lg border-notebook bg-white hover:bg-stone-100 text-stone-700 text-xs font-bold flex items-center justify-center gap-2 transition cursor-pointer"
            >
              <LogoutOutlined />
              <span>{t('nav.logout')}</span>
            </button>
          )}
        </div>
      </aside>

      <main className="flex-1 p-4 md:p-6 overflow-y-auto graph-paper">
        <div className="w-[90%] max-w-[95%] mx-auto">
          {restoredTargetUserId !== user?.id && <p className="p-5 text-stone-600">{t('common.loading')}</p>}
          {restoredTargetUserId === user?.id && requiresStudyGoal && !studyTarget &&
            <StudyGoalRequired onChooseGoal={() => setIsTargetModalOpen(true)} />}
          {restoredTargetUserId === user?.id && (!requiresStudyGoal || !!studyTarget) && <>
          {!materialsReady && requiresMaterials && <p className="p-5 text-stone-600">{language === 'vi' ? 'Đang tải giáo trình...' : 'Loading curriculum...'}</p>}
          {materialsReady && requiresMaterials && materialsError && <div role="alert" className="rounded-xl border border-rose-300 bg-white p-5 text-sm">
            <p>{language === 'vi' ? 'Không tải được nội dung học. Vui lòng thử lại.' : 'Could not load learning content. Please try again.'}</p>
            <button type="button" onClick={() => { materialCache.current = null; setMaterialsReloadKey((value) => value + 1); }} className="mt-3 rounded-lg bg-amber-300 px-4 py-2 font-bold">{language === 'vi' ? 'Thử lại' : 'Retry'}</button>
          </div>}
          {(materialsReady || !requiresMaterials) && (!requiresMaterials || !materialsError) && <React.Fragment key={requiresMaterials ? materialsVersion : undefined}>
          <React.Suspense fallback={<p className="p-5 text-stone-600">{t('common.loading')}</p>}>
          {currentView === 'home' && (
            <HomeView
              target={studyTarget}
              user={user}
              onOpenTargetModal={() => setIsTargetModalOpen(true)}
              onNavigate={(v) => handleNavigate(v)}
              onResume={(courseId, lessonId, stepId) => navigate(`/learn/${encodeURIComponent(courseId)}/${encodeURIComponent(lessonId)}${stepId ? `?step=${encodeURIComponent(stepId)}` : ''}`)}
            />
          )}

          {currentView === 'roadmap' && (
            <LearningRoadmapView
              target={studyTarget}
              onNavigate={(v) => handleNavigate(v)}
              onOpenTargetModal={() => setIsTargetModalOpen(true)}
              onResume={(courseId, lessonId) => navigate(`/learn/${encodeURIComponent(courseId)}/${encodeURIComponent(lessonId)}`)}
            />
          )}

          {currentView === 'learn' && (() => {
            const [, , courseId, lessonId] = location.pathname.split('/');
            return <LessonPlayerView courseId={courseId} lessonId={lessonId} target={studyTarget}
              requestedStep={new URLSearchParams(location.search).get('step')} />;
          })()}

          {currentView === 'courses' && (
            <VocabularyHubView target={studyTarget} onReview={() => handleNavigate('srsReview')} />
          )}

          {currentView === 'curriculum' && (
            <CourseCatalogView target={studyTarget} isAdmin={isAdminUser(user)} onStartQuiz={handleStartQuiz} />
          )}

          {currentView === 'characterLab' && (
            <CharacterLabView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'library' && (
            <LibraryView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'leaderboard' && (
            <LeaderboardView
              user={user}
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'listening' && (
            <ListeningView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'speaking' && (
            <SpeakingReadingView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'reading' && (
            <ReadingView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'writing' && (
            <TopicWritingView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'grammar' && (
            <GrammarView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'aiChat' && (
            <AiConversationView
              target={studyTarget}
              onBack={handleBack}
            />
          )}

          {currentView === 'certificatePractice' && <SpecialPracticeView kind="certificate" target={studyTarget} onBack={handleBack} />}
          {currentView === 'interviewPractice' && <SpecialPracticeView kind="interview" target={studyTarget} onBack={handleBack} />}

          {currentView === 'decks' && (
            <MyDecksView
              target={studyTarget}
              onStudyDeck={handleStudyDeck}
              onOpenCsvImport={(deckId) => setCsvDeckId(deckId)}
            />
          )}

          {currentView === 'srsReview' && (
            <SrsReviewView
              target={studyTarget}
              activeDeckId={selectedStudyDeckId}
              onLearnNew={() => handleNavigate('courses')}
            />
          )}

          {currentView === 'quiz' && (
            activeQuizLesson ? (
              <QuizPlayerView
                lessonId={activeQuizLesson.id}
                lessonTitle={activeQuizLesson.title}
                onBack={() => setActiveQuizLesson(null)}
              />
            ) : (
              <QuizHubView
                target={studyTarget}
                onStartQuiz={(id, title) => handleStartQuiz(id, title)}
                onNavigateToCourses={() => handleNavigate('curriculum')}
                onStartPlacement={() => setIsTargetModalOpen(true)}
              />
            )
          )}

          {currentView === 'statistics' && <StatisticsView />}

          {currentView === 'sharing' && (
            <PublicDecksView target={studyTarget} onDeckCloned={() => handleNavigate('decks')} />
          )}

          {currentView === 'sync' && <SyncView />}

          {currentView === 'admin' && isAuthenticated && isAdminUser(user) && (
            <AdminView key={studyTarget?.languageId || 'all'} target={studyTarget} />
          )}
          </React.Suspense>
          </React.Fragment>}
          </>}
        </div>
      </main>

      <StudyTargetModal
        isOpen={isTargetModalOpen}
        isAdmin={isAdminUser(user)}
        currentTarget={studyTarget}
        allowClose={true}
        onClose={() => setIsTargetModalOpen(false)}
        onSave={handleSaveStudyTarget}
      />

      <QuickDictionaryModal
        isOpen={isDictionaryOpen}
        onClose={() => setIsDictionaryOpen(false)}
        initialWord={dictionaryLookupWord}
        target={studyTarget}
      />
      <NotebookWidget
        isAuthenticated={isAuthenticated}
        onRequestLogin={() => setIsAuthModalOpen(true)}
        languageCode={studyTarget?.languageCode}
        sourcePath={currentPath}
        onDictionary={(word) => { setDictionaryLookupWord(word); setIsDictionaryOpen(true); }}
      />

      <AuthModal
        isOpen={isAuthModalOpen}
        onClose={() => setIsAuthModalOpen(false)}
        onLoginSuccess={handleLoginSuccess}
      />
      <SettingsModal
        isOpen={isSettingsModalOpen}
        onClose={() => setIsSettingsModalOpen(false)}
      />
      {csvDeckId && (
        <CsvWizardModal
          isOpen={!!csvDeckId}
          deckId={csvDeckId}
          onClose={() => setCsvDeckId(null)}
          onSuccess={() => {
            setCsvDeckId(null);
            handleNavigate('decks');
          }}
        />
      )}
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <ConfigProvider
      theme={{
        token: {
          colorPrimary: '#1c1917',
          borderRadius: 8,
          fontFamily: 'inherit'
        }
      }}
    >
      <AntdApp>
        <LanguageProvider>
          <AuthProvider>
            <BrowserRouter>
              <MainShell />
            </BrowserRouter>
          </AuthProvider>
        </LanguageProvider>
      </AntdApp>
    </ConfigProvider>
  );
};

export default App;
