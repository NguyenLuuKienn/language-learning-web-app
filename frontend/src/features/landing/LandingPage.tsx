import React, { useState } from 'react';
import { useTranslation } from '../../i18n';
import { useAuth } from '../auth';
import {
  RocketOutlined,
  ThunderboltOutlined,
  BookOutlined,
  EditOutlined,
  SoundOutlined,
  CopyOutlined,
  CloudSyncOutlined,
  SafetyCertificateOutlined,
  UserOutlined,
  CheckCircleFilled,
  GlobalOutlined,
  ArrowRightOutlined,
  FireOutlined,
  RightOutlined,
  BulbOutlined,
  StarFilled,
  SwapOutlined
} from '@ant-design/icons';
import { isAdminUser } from '../../types';

interface LandingPageProps {
  onEnterApp: (targetView?: 'home' | 'roadmap' | 'courses' | 'decks' | 'writing' | 'speaking' | 'srsReview' | 'sync' | 'admin') => void;
  onOpenAuth: () => void;
  onOpenTargetModal: () => void;
}

export const LandingPage: React.FC<LandingPageProps> = ({
  onEnterApp,
  onOpenAuth,
  onOpenTargetModal
}) => {
  const { language, toggleLanguage } = useTranslation();
  const { user, isAuthenticated, logout } = useAuth();

  const handleEnterAppOrAuth = (
    target: 'home' | 'roadmap' | 'courses' | 'decks' | 'writing' | 'speaking' | 'srsReview' | 'sync' | 'admin' = 'home'
  ) => {
    if (!isAuthenticated) {
      onOpenAuth();
      return;
    }
    if (isAdminUser(user)) {
      onEnterApp(target === 'home' ? 'admin' : target);
    } else {
      onEnterApp(target);
    }
  };

  // Interactive demo card state
  const [isFlipped, setIsFlipped] = useState(false);
  const [demoFeedback, setDemoFeedback] = useState<string | null>(null);

  const demoCard = {
    front: 'Latency',
    phonetic: '/ˈleɪ.tən.si/',
    back: 'Độ trễ truyền tải dữ liệu qua hệ thống mạng',
    example: 'Optimizing database queries reduced the latency from 250ms to 8ms.'
  };

  const handleDemoRating = (rating: string) => {
    setDemoFeedback(rating);
    setTimeout(() => {
      setIsFlipped(false);
      setDemoFeedback(null);
    }, 1800);
  };

  return (
    <div className="min-h-screen bg-[#FAF6EE] text-[#1F1F1F] font-sans selection:bg-amber-200 selection:text-stone-900">
      {/* Top Navigation Bar */}
      <header className="sticky top-0 z-40 bg-[#FAF6EE]/90 backdrop-blur-md border-b-2 border-[#262626]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="bg-[#E11D48] text-white text-xs font-mono font-bold px-2 py-0.5 rounded border border-[#9F1239] shadow-2xs">
              SRS
            </span>
            <div className="leading-tight">
              <span className="font-extrabold text-lg tracking-tight text-stone-900">
                LinguaFlow
              </span>
              <span className="text-[10px] uppercase font-bold text-stone-500 block -mt-1 tracking-wider font-mono">
                LANGUAGE LEARNING PLATFORM
              </span>
            </div>
          </div>

          <nav className="hidden md:flex items-center gap-6 text-sm font-bold text-stone-700">
            <a href="#features" className="hover:text-stone-950 transition">
              {language === 'vi' ? 'Tính năng' : 'Features'}
            </a>
            <a href="#srs" className="hover:text-stone-950 transition">
              {language === 'vi' ? 'Phương pháp SRS' : 'SRS Method'}
            </a>
            <a href="#courses" className="hover:text-stone-950 transition">
              {language === 'vi' ? 'Khóa học' : 'Curriculum'}
            </a>
            <a href="#benefits" className="hover:text-stone-950 transition">
              {language === 'vi' ? 'Lợi ích' : 'Benefits'}
            </a>
          </nav>

          <div className="flex items-center gap-3">
            <button
              onClick={toggleLanguage}
              className="px-2.5 py-1 text-xs font-mono font-bold rounded-lg border-notebook bg-white hover:bg-stone-100 transition cursor-pointer shadow-2xs flex items-center gap-1.5"
              title="Chuyển đổi ngôn ngữ (EN / VI)"
            >
              <GlobalOutlined />
              <span>{language.toUpperCase()}</span>
            </button>

            {isAuthenticated && user ? (
              <div className="flex items-center gap-2">
                <div className="hidden sm:flex items-center gap-2 pl-2 pr-3 py-1 bg-white border-notebook rounded-lg shadow-2xs">
                  <div className="w-6 h-6 rounded-full bg-stone-900 text-white flex items-center justify-center text-xs font-bold">
                    {user.displayName.charAt(0).toUpperCase()}
                  </div>
                  <div className="text-left">
                    <span className="text-xs font-bold text-stone-900 block leading-tight max-w-[120px] truncate">
                      {user.displayName}
                    </span>
                    <span className="text-[9px] font-mono font-semibold text-rose-600 block leading-none">
                      {isAdminUser(user) ? 'Admin' : 'Learner'}
                    </span>
                  </div>
                </div>

                {isAdminUser(user) ? (
                  <button
                    onClick={() => onEnterApp('admin')}
                    className="px-3.5 py-2 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold notebook-btn flex items-center gap-1.5 shadow-xs transition cursor-pointer"
                  >
                    <SafetyCertificateOutlined />
                    <span>{language === 'vi' ? 'Trang Quản Trị' : 'Admin Center'}</span>
                  </button>
                ) : (
                  <button
                    onClick={() => onEnterApp('home')}
                    className="px-3.5 py-2 rounded-xl bg-stone-900 hover:bg-stone-800 text-white text-xs font-bold notebook-btn flex items-center gap-1.5 shadow-xs transition cursor-pointer"
                  >
                    <RocketOutlined />
                    <span>{language === 'vi' ? 'Vào Bàn Học' : 'Study Hub'}</span>
                  </button>
                )}

                <button
                  onClick={() => void logout()}
                  className="px-2.5 py-2 text-xs font-bold text-stone-600 hover:text-stone-950 bg-stone-100 hover:bg-stone-200 rounded-lg transition cursor-pointer"
                  title="Đăng xuất"
                >
                  {language === 'vi' ? 'Thoát' : 'Logout'}
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <button
                  onClick={onOpenAuth}
                  className="px-3.5 py-1.5 text-xs font-bold text-stone-700 hover:text-stone-950 bg-white border-notebook hover:bg-stone-50 rounded-lg transition cursor-pointer shadow-2xs"
                >
                  <UserOutlined className="mr-1" />
                  {language === 'vi' ? 'Đăng nhập' : 'Login'}
                </button>
                <button
                  onClick={() => handleEnterAppOrAuth('home')}
                  className="px-3.5 py-2 bg-stone-900 hover:bg-stone-800 text-white text-xs font-bold rounded-xl notebook-btn flex items-center gap-1.5 shadow-xs transition cursor-pointer"
                >
                  <RocketOutlined />
                  <span>{language === 'vi' ? 'Vào bàn học' : 'Get Started'}</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative overflow-hidden py-16 lg:py-24 border-b-2 border-[#262626] graph-paper">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            {/* Left Column: Headline and Call-to-actions */}
            <div className="lg:col-span-7 space-y-6">
              <div className="inline-flex items-center gap-2 px-3 py-1 bg-amber-100 border-2 border-amber-300 rounded-full text-xs font-bold text-amber-900 shadow-2xs">
                <ThunderboltOutlined className="text-amber-600" />
                <span>
                  {language === 'vi'
                    ? 'Nền tảng Học Ngoại ngữ Cá nhân hóa & Thuật toán SRS'
                    : 'Personalized Language Learning with SRS Algorithm'}
                </span>
              </div>

              <h1 className="text-3xl sm:text-5xl lg:text-6xl font-black text-stone-950 tracking-tight leading-[1.15]">
                {language === 'vi' ? (
                  <>
                    Làm chủ Ngôn ngữ với{' '}
                    <span className="text-[#E11D48] underline decoration-wavy decoration-[#9F1239]/40 underline-offset-8">
                      Khoa học Trí nhớ
                    </span>{' '}
                    Đỉnh cao.
                  </>
                ) : (
                  <>
                    Master Languages with{' '}
                    <span className="text-[#E11D48] underline decoration-wavy decoration-[#9F1239]/40 underline-offset-8">
                      Memory Science
                    </span>
                    .
                  </>
                )}
              </h1>

              <p className="text-base sm:text-lg text-stone-700 leading-relaxed max-w-2xl font-medium">
                {language === 'vi'
                  ? 'Chấm dứt tình trạng học vẹt và mau quên. LinguaFlow ứng dụng thuật toán lặp lại ngắt quãng (Spaced Repetition SM-2), kết hợp bài học từ vựng thực chiến, luyện viết theo chuyên ngành và luyện nói chuẩn giọng bản ngữ.'
                  : 'Stop cramming and forgetting. LinguaFlow applies spaced repetition science (SM-2), pairing high-impact vocabulary with domain-specific writing, speaking, and offline synchronization.'}
              </p>

              <div className="flex flex-wrap items-center gap-4 pt-2">
                <button
                  onClick={() => handleEnterAppOrAuth('home')}
                  className="px-6 py-3.5 bg-stone-900 hover:bg-stone-800 text-white font-bold text-sm sm:text-base rounded-2xl notebook-btn flex items-center gap-2.5 shadow-md transition cursor-pointer"
                >
                  <RocketOutlined />
                  <span>
                    {isAuthenticated
                      ? isAdminUser(user)
                        ? language === 'vi'
                          ? 'Vào Trang Quản Trị'
                          : 'Go to Admin Center'
                        : language === 'vi'
                        ? 'Vào Bàn Học Ngay'
                        : 'Go to Study Workspace'
                      : language === 'vi'
                      ? 'Vào Bàn Học Ngay'
                      : 'Start Learning Free'}
                  </span>
                  <ArrowRightOutlined />
                </button>

                <button
                  onClick={onOpenTargetModal}
                  className="px-5 py-3.5 bg-white border-notebook hover:bg-amber-50/70 text-stone-800 font-bold text-sm sm:text-base rounded-2xl flex items-center gap-2 transition cursor-pointer shadow-2xs"
                >
                  <GlobalOutlined className="text-amber-600" />
                  <span>
                    {language === 'vi' ? 'Chọn Mục tiêu Học tập' : 'Set Learning Target'}
                  </span>
                </button>
              </div>

              {/* Quick Trust Badges */}
              <div className="pt-4 flex flex-wrap items-center gap-6 text-xs font-bold text-stone-600">
                <div className="flex items-center gap-1.5">
                  <CheckCircleFilled className="text-emerald-600" />
                  <span>{language === 'vi' ? 'Học Ngoại tuyến (Offline-First)' : 'Offline Support'}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <CheckCircleFilled className="text-emerald-600" />
                  <span>{language === 'vi' ? 'Thuật toán SRS Khoa học' : 'SM-2 Algorithm'}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <CheckCircleFilled className="text-emerald-600" />
                  <span>{language === 'vi' ? 'Đa Chuyên ngành & Trình độ' : 'Domain-Specific Tracks'}</span>
                </div>
              </div>
            </div>

            {/* Right Column: Live Interactive SRS Card Demo */}
            <div className="lg:col-span-5 flex justify-center">
              <div className="w-full max-w-md bg-white border-notebook-2 rounded-3xl p-6 shadow-notebook relative group">
                <div className="flex items-center justify-between border-b-2 border-stone-200 pb-3 mb-4">
                  <div className="flex items-center gap-2">
                    <span className="w-3 h-3 rounded-full bg-rose-500 inline-block" />
                    <span className="w-3 h-3 rounded-full bg-amber-500 inline-block" />
                    <span className="w-3 h-3 rounded-full bg-emerald-500 inline-block" />
                    <span className="text-[11px] font-mono font-bold text-stone-500 uppercase ml-1">
                      {language === 'vi' ? 'Trải nghiệm thử Flashcard SRS' : 'Interactive SRS Demo'}
                    </span>
                  </div>
                  <span className="text-[11px] font-mono font-bold bg-amber-100 text-amber-900 px-2 py-0.5 rounded border border-amber-300">
                    B1 • IT ENGLISH
                  </span>
                </div>

                {/* Flip Card Container */}
                <div
                  onClick={() => setIsFlipped(!isFlipped)}
                  className="min-h-[220px] rounded-2xl border-2 border-dashed border-stone-300 hover:border-stone-900 bg-[#FAF6EE] p-6 flex flex-col justify-between cursor-pointer transition-all duration-300 relative select-none"
                >
                  <div className="flex items-center justify-between text-xs font-mono text-stone-500">
                    <span>{isFlipped ? (language === 'vi' ? 'MẶT SAU (ĐÁP ÁN)' : 'BACK SIDE') : (language === 'vi' ? 'MẶT TRƯỚC (THUẬT NGỮ)' : 'FRONT SIDE')}</span>
                    <span className="flex items-center gap-1 text-stone-600 font-bold">
                      <SwapOutlined /> {language === 'vi' ? 'Chạm để lật' : 'Click to flip'}
                    </span>
                  </div>

                  {!isFlipped ? (
                    <div className="my-auto text-center py-4">
                      <h3 className="text-3xl font-black text-stone-950 tracking-tight">
                        {demoCard.front}
                      </h3>
                      <p className="text-sm font-mono text-stone-500 mt-1">
                        {demoCard.phonetic}
                      </p>
                      <span className="inline-block mt-3 text-xs text-stone-500 bg-white px-3 py-1 rounded-full border border-stone-200">
                        {language === 'vi' ? 'Bấm để xem định nghĩa tiếng Việt' : 'Click to reveal definition'}
                      </span>
                    </div>
                  ) : (
                    <div className="my-auto text-center py-4 space-y-2">
                      <h4 className="text-lg font-extrabold text-stone-900">
                        {demoCard.back}
                      </h4>
                      <p className="text-xs text-stone-600 italic bg-white/80 p-2.5 rounded-xl border border-stone-200 text-left">
                        &quot;{demoCard.example}&quot;
                      </p>
                    </div>
                  )}

                  <div className="text-[10px] text-center font-mono text-stone-400">
                    {language === 'vi' ? 'Ví dụ cách lên lịch ôn tập • Thẻ đến hạn' : 'Review scheduling example • Card due now'}
                  </div>
                </div>

                {/* Simulated SRS Feedback Buttons */}
                <div className="mt-4 pt-3 border-t border-stone-200">
                  <div className="text-xs font-bold text-stone-700 text-center mb-2">
                    {language === 'vi' ? 'Đánh giá mức độ nhớ của bạn:' : 'Rate your recall quality:'}
                  </div>
                  <div className="grid grid-cols-4 gap-1.5">
                    <button
                      onClick={() => handleDemoRating('Lặp lại (1 ngày)')}
                      className="py-1.5 rounded-lg bg-red-100 hover:bg-red-200 text-red-800 text-[11px] font-bold border border-red-300 transition cursor-pointer"
                    >
                      {language === 'vi' ? 'Quên' : 'Again'}
                    </button>
                    <button
                      onClick={() => handleDemoRating('Khó (2 ngày)')}
                      className="py-1.5 rounded-lg bg-amber-100 hover:bg-amber-200 text-amber-800 text-[11px] font-bold border border-amber-300 transition cursor-pointer"
                    >
                      {language === 'vi' ? 'Khó' : 'Hard'}
                    </button>
                    <button
                      onClick={() => handleDemoRating('Tốt (4 ngày)')}
                      className="py-1.5 rounded-lg bg-blue-100 hover:bg-blue-200 text-blue-800 text-[11px] font-bold border border-blue-300 transition cursor-pointer"
                    >
                      {language === 'vi' ? 'Nhớ' : 'Good'}
                    </button>
                    <button
                      onClick={() => handleDemoRating('Dễ (7 ngày)')}
                      className="py-1.5 rounded-lg bg-emerald-100 hover:bg-emerald-200 text-emerald-800 text-[11px] font-bold border border-emerald-300 transition cursor-pointer"
                    >
                      {language === 'vi' ? 'Dễ' : 'Easy'}
                    </button>
                  </div>

                  {demoFeedback && (
                    <div className="mt-2 text-center text-xs font-bold text-emerald-700 bg-emerald-50 py-1 rounded border border-emerald-200 animate-fade-in">
                      ✓ Đã lên lịch ôn tiếp theo: {demoFeedback}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Metrics & Statistics Strip */}
      <section className="bg-white border-b-2 border-[#262626] py-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
            <div className="p-4 border-r-0 md:border-r border-stone-200">
              <div className="text-3xl sm:text-4xl font-black text-stone-950 font-mono">
                98.4%
              </div>
              <div className="text-xs sm:text-sm font-bold text-stone-600 mt-1">
                {language === 'vi' ? 'Tỷ lệ ghi nhớ dài hạn' : 'Long-term Retention'}
              </div>
            </div>
            <div className="p-4 border-r-0 md:border-r border-stone-200">
              <div className="text-3xl sm:text-4xl font-black text-rose-600 font-mono">
                50,000+
              </div>
              <div className="text-xs sm:text-sm font-bold text-stone-600 mt-1">
                {language === 'vi' ? 'Lượt thẻ ôn tập qua SRS' : 'Flashcards Reviewed'}
              </div>
            </div>
            <div className="p-4 border-r-0 md:border-r border-stone-200">
              <div className="text-3xl sm:text-4xl font-black text-amber-600 font-mono">
                12+
              </div>
              <div className="text-xs sm:text-sm font-bold text-stone-600 mt-1">
                {language === 'vi' ? 'Chuyên ngành thực chiến' : 'Specialized Domains'}
              </div>
            </div>
            <div className="p-4">
              <div className="text-3xl sm:text-4xl font-black text-stone-950 font-mono">
                100%
              </div>
              <div className="text-xs sm:text-sm font-bold text-stone-600 mt-1">
                {language === 'vi' ? 'Hỗ trợ Học Offline & Sync' : 'Offline Capable'}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Core Features Grid */}
      <section id="features" className="py-20 border-b-2 border-[#262626]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-3xl mx-auto mb-14">
            <span className="text-xs font-mono font-bold uppercase tracking-wider text-amber-800 bg-amber-100 px-3 py-1 rounded-full border border-amber-300">
              {language === 'vi' ? 'TÍNH NĂNG TOÀN DIỆN' : 'POWERFUL FEATURES'}
            </span>
            <h2 className="text-3xl sm:text-4xl font-black text-stone-950 mt-3 tracking-tight">
              {language === 'vi'
                ? 'Mọi công cụ bạn cần để thành thạo một ngôn ngữ'
                : 'Everything you need to master a new language'}
            </h2>
            <p className="text-stone-600 text-sm sm:text-base mt-2">
              {language === 'vi'
                ? 'Phương pháp học hiện đại tích hợp đa giác quan: Nghe, Nói, Đọc, Viết và Ghi nhớ khoa học.'
                : 'A modern multi-sensory learning approach: Listening, Speaking, Reading, Writing, and Memory retention.'}
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {/* Feature 1: SRS Flashcards */}
            <div className="p-6 bg-white border-notebook rounded-2xl shadow-notebook hover:-translate-y-1 transition duration-200 flex flex-col justify-between">
              <div>
                <div className="w-12 h-12 rounded-xl bg-rose-100 border border-rose-300 text-rose-600 flex items-center justify-center text-xl mb-4 font-bold">
                  <ThunderboltOutlined />
                </div>
                <h3 className="text-lg font-extrabold text-stone-900 mb-2">
                  {language === 'vi' ? 'Ôn tập Thông minh SRS' : 'Spaced Repetition System'}
                </h3>
                <p className="text-xs sm:text-sm text-stone-600 leading-relaxed">
                  {language === 'vi'
                    ? 'Thuật toán SM-2 tự động tính toán thời điểm sắp quên để nhắc bạn ôn tập thẻ flashcard, biến trí nhớ ngắn hạn thành phản xạ dài hạn.'
                    : 'The SM-2 algorithm calculates the optimal review interval before you forget, locking vocabulary into permanent memory.'}
                </p>
              </div>
              <button
                onClick={() => handleEnterAppOrAuth('srsReview')}
                className="mt-6 text-xs font-bold text-rose-600 hover:text-rose-800 flex items-center gap-1 cursor-pointer"
              >
                <span>{language === 'vi' ? 'Thử ôn tập ngay' : 'Try SRS Review'}</span>
                <RightOutlined />
              </button>
            </div>

            {/* Feature 2: Structured Curriculum */}
            <div className="p-6 bg-white border-notebook rounded-2xl shadow-notebook hover:-translate-y-1 transition duration-200 flex flex-col justify-between">
              <div>
                <div className="w-12 h-12 rounded-xl bg-blue-100 border border-blue-300 text-blue-600 flex items-center justify-center text-xl mb-4 font-bold">
                  <BookOutlined />
                </div>
                <h3 className="text-lg font-extrabold text-stone-900 mb-2">
                  {language === 'vi' ? 'Khóa học theo Trình độ & Lĩnh vực' : 'Level & Domain Curriculum'}
                </h3>
                <p className="text-xs sm:text-sm text-stone-600 leading-relaxed">
                  {language === 'vi'
                    ? 'Giáo trình chuẩn từ A1 đến C1 được phân loại theo chuyên ngành như Công nghệ thông tin, Thương mại, Y khoa, Du lịch và Đời sống.'
                    : 'Structured courses from A1 to C1 categorized by specific industries including IT, Commerce, Medical, Tourism, and Daily life.'}
                </p>
              </div>
              <button
                onClick={() => handleEnterAppOrAuth('roadmap')}
                className="mt-6 text-xs font-bold text-blue-600 hover:text-blue-800 flex items-center gap-1 cursor-pointer"
              >
                <span>{language === 'vi' ? 'Khám phá khóa học' : 'Explore Courses'}</span>
                <RightOutlined />
              </button>
            </div>

            {/* Feature 3: Writing Practice */}
            <div className="p-6 bg-white border-notebook rounded-2xl shadow-notebook hover:-translate-y-1 transition duration-200 flex flex-col justify-between">
              <div>
                <div className="w-12 h-12 rounded-xl bg-emerald-100 border border-emerald-300 text-emerald-600 flex items-center justify-center text-xl mb-4 font-bold">
                  <EditOutlined />
                </div>
                <h3 className="text-lg font-extrabold text-stone-900 mb-2">
                  {language === 'vi' ? 'Luyện viết theo Chủ đề' : 'Topic Writing Studio'}
                </h3>
                <p className="text-xs sm:text-sm text-stone-600 leading-relaxed">
                  {language === 'vi'
                    ? 'Viết email công việc, thông báo dự án, và bài luận ngắn với gợi ý từ vựng chuyên ngành và đếm từ thời gian thực.'
                    : 'Compose professional emails, technical specs, and short essays with integrated vocabulary suggestions and live word metrics.'}
                </p>
              </div>
              <button
                onClick={() => handleEnterAppOrAuth('writing')}
                className="mt-6 text-xs font-bold text-emerald-600 hover:text-emerald-800 flex items-center gap-1 cursor-pointer"
              >
                <span>{language === 'vi' ? 'Vào phòng luyện viết' : 'Practice Writing'}</span>
                <RightOutlined />
              </button>
            </div>

            {/* Feature 4: Speaking & Pronunciation */}
            <div className="p-6 bg-white border-notebook rounded-2xl shadow-notebook hover:-translate-y-1 transition duration-200 flex flex-col justify-between">
              <div>
                <div className="w-12 h-12 rounded-xl bg-purple-100 border border-purple-300 text-purple-600 flex items-center justify-center text-xl mb-4 font-bold">
                  <SoundOutlined />
                </div>
                <h3 className="text-lg font-extrabold text-stone-900 mb-2">
                  {language === 'vi' ? 'Luyện phát âm & Đọc hiểu' : 'Speaking & Reading Lab'}
                </h3>
                <p className="text-xs sm:text-sm text-stone-600 leading-relaxed">
                  {language === 'vi'
                    ? 'Nghe giọng đọc chuẩn bản ngữ, luyện nói và phát âm với trợ lý âm thanh, so sánh độ chính xác của ngữ điệu.'
                    : 'Listen to native voice synthesis, record and practice pronunciation, and refine articulation rhythm.'}
                </p>
              </div>
              <button
                onClick={() => handleEnterAppOrAuth('speaking')}
                className="mt-6 text-xs font-bold text-purple-600 hover:text-purple-800 flex items-center gap-1 cursor-pointer"
              >
                <span>{language === 'vi' ? 'Luyện nói ngay' : 'Practice Speaking'}</span>
                <RightOutlined />
              </button>
            </div>

            {/* Feature 5: Flashcard Decks & Sharing */}
            <div className="p-6 bg-white border-notebook rounded-2xl shadow-notebook hover:-translate-y-1 transition duration-200 flex flex-col justify-between">
              <div>
                <div className="w-12 h-12 rounded-xl bg-amber-100 border border-amber-300 text-amber-700 flex items-center justify-center text-xl mb-4 font-bold">
                  <CopyOutlined />
                </div>
                <h3 className="text-lg font-extrabold text-stone-900 mb-2">
                  {language === 'vi' ? 'Bộ thẻ Tùy biến & Import CSV' : 'Custom Decks & CSV Wizard'}
                </h3>
                <p className="text-xs sm:text-sm text-stone-600 leading-relaxed">
                  {language === 'vi'
                    ? 'Tự tạo bộ thẻ riêng, import hàng trăm từ vựng từ Excel/CSV trong 1 click, hoặc nhân bản bộ thẻ công khai chất lượng.'
                    : 'Create bespoke flashcards, import hundreds of entries via CSV/Excel in seconds, or clone curated public decks.'}
                </p>
              </div>
              <button
                onClick={() => handleEnterAppOrAuth('decks')}
                className="mt-6 text-xs font-bold text-amber-700 hover:text-amber-900 flex items-center gap-1 cursor-pointer"
              >
                <span>{language === 'vi' ? 'Xem bộ thẻ' : 'Manage Decks'}</span>
                <RightOutlined />
              </button>
            </div>

            {/* Feature 6: Offline & Cloud Sync */}
            <div className="p-6 bg-white border-notebook rounded-2xl shadow-notebook hover:-translate-y-1 transition duration-200 flex flex-col justify-between">
              <div>
                <div className="w-12 h-12 rounded-xl bg-cyan-100 border border-cyan-300 text-cyan-700 flex items-center justify-center text-xl mb-4 font-bold">
                  <CloudSyncOutlined />
                </div>
                <h3 className="text-lg font-extrabold text-stone-900 mb-2">
                  {language === 'vi' ? 'Học Offline & Đồng bộ 2 Chiều' : 'Offline First & Two-Way Sync'}
                </h3>
                <p className="text-xs sm:text-sm text-stone-600 leading-relaxed">
                  {language === 'vi'
                    ? 'Dữ liệu lưu an toàn trên trình duyệt IndexedDB. Bạn có thể học trên máy bay, tàu điện và đồng bộ lên server khi có mạng.'
                    : 'Offline data stored securely in browser IndexedDB. Continue learning without internet; changes sync seamlessly when online.'}
                </p>
              </div>
              <button
                onClick={() => handleEnterAppOrAuth('sync')}
                className="mt-6 text-xs font-bold text-cyan-700 hover:text-cyan-900 flex items-center gap-1 cursor-pointer"
              >
                <span>{language === 'vi' ? 'Xem trung tâm đồng bộ' : 'Sync Center'}</span>
                <RightOutlined />
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* The Science of SRS Section */}
      <section id="srs" className="py-20 bg-white border-b-2 border-[#262626]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            <div className="lg:col-span-6 space-y-5">
              <span className="text-xs font-mono font-bold uppercase tracking-wider text-rose-800 bg-rose-100 px-3 py-1 rounded-full border border-rose-300">
                {language === 'vi' ? 'CƠ SỞ KHOA HỌC' : 'THE SCIENCE BEHIND'}
              </span>
              <h2 className="text-3xl sm:text-4xl font-black text-stone-950 tracking-tight">
                {language === 'vi'
                  ? 'Tại sao 90% người học ngoại ngữ quên từ vựng sau 7 ngày?'
                  : 'Why do 90% of learners forget vocabulary within 7 days?'}
              </h2>
              <p className="text-stone-700 text-sm sm:text-base leading-relaxed">
                {language === 'vi'
                  ? 'Theo đường cong lãng quên của Hermann Ebbinghaus, não người mất tới 75% thông tin mới chỉ sau 48 giờ nếu không được củng cố. Thuật toán SRS của LinguaFlow can thiệp chính xác vào thời điểm từ vựng bắt đầu mờ nhạt, kéo giãn thời gian ghi nhớ lên 2x, 4x, 10x.'
                  : 'According to Ebbinghaus Forgetting Curve, memory declines sharply within 48 hours without reinforcement. LinguaFlow schedules each review session at the exact moment of decline, expanding retention by 2x, 4x, and 10x.'}
              </p>

              <div className="space-y-3 pt-2">
                <div className="flex items-start gap-3 p-3 rounded-xl bg-[#FAF6EE] border-notebook">
                  <span className="w-6 h-6 rounded-full bg-rose-600 text-white flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
                    1
                  </span>
                  <div>
                    <h4 className="text-xs font-bold text-stone-900">
                      {language === 'vi' ? 'Chu kỳ 1: Sau 1 ngày' : 'Interval 1: After 24 hours'}
                    </h4>
                    <p className="text-[11px] text-stone-600">
                      {language === 'vi' ? 'Củng cố ấn tượng đầu tiên về phát âm và ngữ nghĩa.' : 'Solidifies the initial neural footprint.'}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3 p-3 rounded-xl bg-[#FAF6EE] border-notebook">
                  <span className="w-6 h-6 rounded-full bg-amber-600 text-white flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
                    2
                  </span>
                  <div>
                    <h4 className="text-xs font-bold text-stone-900">
                      {language === 'vi' ? 'Chu kỳ 2: Sau 3 - 5 ngày' : 'Interval 2: After 3 - 5 days'}
                    </h4>
                    <p className="text-[11px] text-stone-600">
                      {language === 'vi' ? 'Chuyển thông tin từ trí nhớ ngắn hạn sang vùng trung hạn.' : 'Transitions knowledge into intermediate memory.'}
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-3 p-3 rounded-xl bg-[#FAF6EE] border-notebook">
                  <span className="w-6 h-6 rounded-full bg-emerald-600 text-white flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
                    3
                  </span>
                  <div>
                    <h4 className="text-xs font-bold text-stone-900">
                      {language === 'vi' ? 'Chu kỳ 3: Sau 14 - 30 ngày' : 'Interval 3: After 14 - 30 days'}
                    </h4>
                    <p className="text-[11px] text-stone-600">
                      {language === 'vi' ? 'Trở thành phản xạ tự nhiên, không cần suy dịch trong đầu.' : 'Becomes permanent reflexive mastery.'}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="lg:col-span-6 bg-[#FAF6EE] border-notebook-2 rounded-3xl p-6 sm:p-8 shadow-notebook">
              <h3 className="text-lg font-black text-stone-900 mb-4 flex items-center gap-2">
                <BulbOutlined className="text-amber-600" />
                <span>{language === 'vi' ? 'Mô hình Đường cong Trí nhớ LinguaFlow' : 'Memory Retention Progression'}</span>
              </h3>

              <div className="space-y-4">
                <div>
                  <div className="flex justify-between text-xs font-bold mb-1">
                    <span className="text-stone-600">{language === 'vi' ? 'Học truyền thống (Học vẹt 1 lần)' : 'Passive Cramming'}</span>
                     <span className="text-rose-600 font-mono">{language === 'vi' ? '18% sau 7 ngày' : '18% after 7 days'}</span>
                  </div>
                  <div className="w-full h-3 bg-stone-200 rounded-full overflow-hidden border border-stone-300">
                    <div className="h-full bg-rose-500 rounded-full w-[18%]" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-xs font-bold mb-1">
                    <span className="text-stone-600">{language === 'vi' ? 'Ôn tập ngẫu nhiên' : 'Random Review'}</span>
                     <span className="text-amber-600 font-mono">{language === 'vi' ? '52% sau 7 ngày' : '52% after 7 days'}</span>
                  </div>
                  <div className="w-full h-3 bg-stone-200 rounded-full overflow-hidden border border-stone-300">
                    <div className="h-full bg-amber-500 rounded-full w-[52%]" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-xs font-bold mb-1">
                    <span className="text-stone-900 font-extrabold flex items-center gap-1">
                      <StarFilled className="text-amber-500 text-xs" />
                      {language === 'vi' ? 'Thuật toán SRS LinguaFlow' : 'LinguaFlow SRS System'}
                    </span>
                     <span className="text-emerald-700 font-mono font-black">{language === 'vi' ? '94.8% sau 30 ngày' : '94.8% after 30 days'}</span>
                  </div>
                  <div className="w-full h-4 bg-stone-200 rounded-full overflow-hidden border-2 border-stone-400 p-0.5">
                    <div className="h-full bg-emerald-500 rounded-full w-[95%]" />
                  </div>
                </div>
              </div>

              <div className="mt-8 p-4 bg-white border-notebook rounded-xl text-center">
                <p className="text-xs font-bold text-stone-800">
                  {language === 'vi'
                    ? '⚡ Chỉ cần 10-15 phút ôn tập mỗi ngày để ghi nhớ hàng nghìn từ vựng.'
                    : '⚡ Just 10-15 minutes per day delivers mastery of thousands of words.'}
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Featured Courses Track */}
      <section id="courses" className="py-20 border-b-2 border-[#262626]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col md:flex-row md:items-end justify-between mb-12 gap-4">
            <div>
              <span className="text-xs font-mono font-bold uppercase tracking-wider text-blue-800 bg-blue-100 px-3 py-1 rounded-full border border-blue-300">
                {language === 'vi' ? 'LỘ TRÌNH ĐA DẠNG' : 'CURRICULUM'}
              </span>
              <h2 className="text-3xl sm:text-4xl font-black text-stone-950 mt-2 tracking-tight">
                {language === 'vi' ? 'Khóa học Đề xuất Cho Bạn' : 'Curated Course Catalog'}
              </h2>
            </div>
            <button
              onClick={() => handleEnterAppOrAuth('roadmap')}
              className="px-4 py-2 bg-white border-notebook hover:bg-stone-50 text-xs font-bold rounded-xl transition cursor-pointer shadow-2xs self-start md:self-auto flex items-center gap-1.5"
            >
              <span>{language === 'vi' ? 'Xem tất cả khóa học' : 'Browse All Courses'}</span>
              <ArrowRightOutlined />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Course Card 1 */}
            <div className="bg-white border-notebook rounded-2xl overflow-hidden shadow-notebook flex flex-col justify-between">
              <div className="p-6">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-xs font-mono font-bold px-2 py-0.5 bg-blue-50 text-blue-700 border border-blue-200 rounded">
                    ENGLISH • A1
                  </span>
                   <span className="text-xs text-stone-500 font-mono">{language === 'vi' ? 'Lộ trình minh họa' : 'Example path'}</span>
                </div>
                <h3 className="text-lg font-bold text-stone-900 mb-2">
                   {language === 'vi' ? 'Tiếng Anh A1 cho người mới học' : 'English A1 for Beginners'}
                </h3>
                <p className="text-xs text-stone-600 line-clamp-3">
                   {language === 'vi' ? 'Luyện chào hỏi, con số và hội thoại cơ bản theo từng bài học.' : 'Practice greetings, numbers and basic conversations lesson by lesson.'}
                </p>
              </div>
              <div className="p-4 bg-[#FAF6EE] border-t border-stone-200 flex items-center justify-between">
                 <span className="text-[11px] font-bold text-emerald-700">✓ {language === 'vi' ? 'Từ vựng và câu hỏi' : 'Vocabulary and questions'}</span>
                <button
                   onClick={() => handleEnterAppOrAuth('roadmap')}
                  className="px-3 py-1.5 bg-stone-900 hover:bg-stone-800 text-white text-xs font-bold rounded-lg transition cursor-pointer"
                >
                  {language === 'vi' ? 'Vào học' : 'Enroll'}
                </button>
              </div>
            </div>

            {/* Course Card 2 */}
            <div className="bg-white border-notebook rounded-2xl overflow-hidden shadow-notebook flex flex-col justify-between">
              <div className="p-6">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-xs font-mono font-bold px-2 py-0.5 bg-purple-50 text-purple-700 border border-purple-200 rounded">
                    IT ENGLISH • B1
                  </span>
                   <span className="text-xs text-stone-500 font-mono">{language === 'vi' ? 'Chuyên ngành' : 'Specialized'}</span>
                </div>
                <h3 className="text-lg font-bold text-stone-900 mb-2">
                   {language === 'vi' ? 'Giao tiếp kỹ thuật CNTT' : 'IT Technical Communications'}
                </h3>
                <p className="text-xs text-stone-600 line-clamp-3">
                   {language === 'vi' ? 'Thuật ngữ cho kỹ sư phần mềm, họp dự án, đánh giá mã và tài liệu API.' : 'Terminology for software engineers, project meetings, code reviews and API documentation.'}
                </p>
              </div>
              <div className="p-4 bg-[#FAF6EE] border-t border-stone-200 flex items-center justify-between">
                 <span className="text-[11px] font-bold text-emerald-700">✓ {language === 'vi' ? 'Chuyên ngành phần mềm' : 'Software engineering'}</span>
                <button
                   onClick={() => handleEnterAppOrAuth('roadmap')}
                  className="px-3 py-1.5 bg-stone-900 hover:bg-stone-800 text-white text-xs font-bold rounded-lg transition cursor-pointer"
                >
                  {language === 'vi' ? 'Vào học' : 'Enroll'}
                </button>
              </div>
            </div>

            {/* Course Card 3 */}
            <div className="bg-white border-notebook rounded-2xl overflow-hidden shadow-notebook flex flex-col justify-between">
              <div className="p-6">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-xs font-mono font-bold px-2 py-0.5 bg-amber-50 text-amber-700 border border-amber-200 rounded">
                    JAPANESE • N5
                  </span>
                   <span className="text-xs text-stone-500 font-mono">{language === 'vi' ? 'Kanji & từ vựng' : 'Kanji & vocabulary'}</span>
                </div>
                <h3 className="text-lg font-bold text-stone-900 mb-2">
                   {language === 'vi' ? 'Nền tảng tiếng Nhật N5' : 'Japanese Foundations N5'}
                </h3>
                <p className="text-xs text-stone-600 line-clamp-3">
                   {language === 'vi' ? 'Hiragana, Katakana, Kanji cơ bản và giao tiếp hằng ngày.' : 'Hiragana, Katakana, basic Kanji and everyday conversation.'}
                </p>
              </div>
              <div className="p-4 bg-[#FAF6EE] border-t border-stone-200 flex items-center justify-between">
                 <span className="text-[11px] font-bold text-emerald-700">✓ {language === 'vi' ? 'Bộ thẻ ghi nhớ' : 'Flashcard deck'}</span>
                <button
                   onClick={() => handleEnterAppOrAuth('roadmap')}
                  className="px-3 py-1.5 bg-stone-900 hover:bg-stone-800 text-white text-xs font-bold rounded-lg transition cursor-pointer"
                >
                  {language === 'vi' ? 'Vào học' : 'Enroll'}
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Call to action section */}
      <section className="py-20 bg-stone-900 text-white relative overflow-hidden">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 text-center space-y-6">
          <div className="inline-flex items-center gap-2 px-3 py-1 bg-stone-800 border border-stone-700 rounded-full text-xs font-mono font-bold text-amber-400">
            <FireOutlined />
            <span>LINGUAFLOW PLATFORM 2026</span>
          </div>

          <h2 className="text-3xl sm:text-5xl font-black tracking-tight leading-tight">
            {language === 'vi'
              ? 'Bắt đầu nâng tầm ngoại ngữ của bạn ngay hôm nay.'
              : 'Start your accelerated language journey today.'}
          </h2>

          <p className="text-stone-300 text-sm sm:text-base max-w-2xl mx-auto">
            {language === 'vi'
              ? 'Tham gia cùng hàng nghìn học viên đang áp dụng phương pháp khoa học để ghi nhớ từ vựng và tự tin giao tiếp.'
              : 'Join learners using scientifically backed spaced repetition to unlock true fluency.'}
          </p>

          <div className="flex flex-wrap items-center justify-center gap-4 pt-4">
            <button
              onClick={() => handleEnterAppOrAuth('home')}
              className="px-8 py-4 bg-amber-400 hover:bg-amber-300 text-stone-950 font-black text-sm sm:text-base rounded-2xl shadow-xl hover:scale-105 transition cursor-pointer flex items-center gap-2"
            >
              <RocketOutlined />
              <span>
                {isAuthenticated
                  ? isAdminUser(user)
                    ? language === 'vi'
                      ? 'Vào Trang Quản Trị'
                      : 'Go to Admin Center'
                    : language === 'vi'
                    ? 'Vào Bàn Học Ngay'
                    : 'Enter Study Workspace'
                  : language === 'vi'
                  ? 'Vào Bàn Học Ngay'
                  : 'Enter Study Workspace'}
              </span>
              <ArrowRightOutlined />
            </button>

            {!isAuthenticated && (
              <button
                onClick={onOpenAuth}
                className="px-6 py-4 bg-stone-800 hover:bg-stone-700 border border-stone-600 text-white font-bold text-sm sm:text-base rounded-2xl transition cursor-pointer"
              >
                {language === 'vi' ? 'Đăng nhập / Đăng ký' : 'Sign In / Register'}
              </button>
            )}
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-8 bg-[#FAF6EE] border-t-2 border-[#262626] text-xs text-stone-600">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="font-extrabold text-stone-900">LinguaFlow</span>
            <span>•</span>
            <span>{language === 'vi' ? 'Nền tảng Học Ngoại ngữ Cá nhân hóa' : 'Personalized Language Learning Platform'}</span>
          </div>
          <div>
            <span>© 2026 LinguaFlow. All rights reserved.</span>
          </div>
        </div>
      </footer>
    </div>
  );
};
