import { useState, useEffect } from 'react';
import {
  Flame,
  Wifi,
  WifiOff,
  BookOpen,
  Layers,
  Clock,
  Sparkles,
  BarChart3,
  RefreshCw,
  ShieldCheck,
  ChevronRight,
  Play,
  RotateCw,
  Volume2,
  CheckCircle2,
  GraduationCap
} from 'lucide-react';

export function App() {
  const [activeTab, setActiveTab] = useState<'courses' | 'srs' | 'stats' | 'offline'>('courses');
  const [isOnline, setIsOnline] = useState(true);
  const [isFlipped, setIsFlipped] = useState(false);
  const [selectedRating, setSelectedRating] = useState<string | null>(null);

  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans">
      {/* Top Notification / Sync Bar */}
      <div className="bg-gradient-to-r from-indigo-900/60 via-purple-900/40 to-slate-900 px-4 py-2 border-b border-indigo-500/20 text-xs flex justify-between items-center backdrop-blur-md">
        <div className="flex items-center gap-2">
          <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full font-medium bg-indigo-500/10 text-indigo-300 border border-indigo-500/20">
            <Sparkles className="w-3.5 h-3.5 text-indigo-400" />
            Đề tài môn PTPMMNM • MVP v1.0.0
          </span>
          <span className="hidden sm:inline text-slate-400">
            Kiến trúc Clean Architecture + React PWA + MySQL
          </span>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setIsOnline(!isOnline)}
            className="flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium transition-all bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700"
            title="Nhấp để mô phỏng chuyển đổi Online/Offline"
          >
            {isOnline ? (
              <>
                <Wifi className="w-3.5 h-3.5 text-emerald-400" />
                <span className="text-emerald-300">Trực tuyến</span>
              </>
            ) : (
              <>
                <WifiOff className="w-3.5 h-3.5 text-amber-400" />
                <span className="text-amber-300">Ngoại tuyến (2 chờ đồng bộ)</span>
              </>
            )}
          </button>
        </div>
      </div>

      {/* Main Header */}
      <header className="sticky top-0 z-50 glass-panel border-b border-slate-800/80">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-600 to-violet-500 flex items-center justify-center shadow-lg shadow-indigo-500/30">
              <GraduationCap className="w-6 h-6 text-white" />
            </div>
            <div>
              <span className="font-extrabold text-xl tracking-tight bg-gradient-to-r from-white via-indigo-200 to-indigo-400 bg-clip-text text-transparent">
                LinguaFlow
              </span>
              <span className="text-[10px] uppercase font-bold tracking-widest text-indigo-400 block -mt-1">
                SRS Learning
              </span>
            </div>
          </div>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-1">
            <button
              onClick={() => setActiveTab('courses')}
              className={`px-3.5 py-2 rounded-lg text-sm font-medium transition-all flex items-center gap-2 ${
                activeTab === 'courses'
                  ? 'bg-indigo-600/20 text-indigo-300 border border-indigo-500/30'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
              }`}
            >
              <BookOpen className="w-4 h-4" />
              Khóa học
            </button>
            <button
              onClick={() => setActiveTab('srs')}
              className={`px-3.5 py-2 rounded-lg text-sm font-medium transition-all flex items-center gap-2 ${
                activeTab === 'srs'
                  ? 'bg-indigo-600/20 text-indigo-300 border border-indigo-500/30'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
              }`}
            >
              <Layers className="w-4 h-4" />
              Bộ thẻ & SRS
            </button>
            <button
              onClick={() => setActiveTab('stats')}
              className={`px-3.5 py-2 rounded-lg text-sm font-medium transition-all flex items-center gap-2 ${
                activeTab === 'stats'
                  ? 'bg-indigo-600/20 text-indigo-300 border border-indigo-500/30'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
              }`}
            >
              <BarChart3 className="w-4 h-4" />
              Thống kê & Streak
            </button>
            <button
              onClick={() => setActiveTab('offline')}
              className={`px-3.5 py-2 rounded-lg text-sm font-medium transition-all flex items-center gap-2 ${
                activeTab === 'offline'
                  ? 'bg-indigo-600/20 text-indigo-300 border border-indigo-500/30'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
              }`}
            >
              <RefreshCw className="w-4 h-4" />
              Offline Sync
            </button>
          </nav>

          {/* Right Actions */}
          <div className="flex items-center gap-3">
            {/* Streak Indicator */}
            <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-amber-500/10 border border-amber-500/30 text-amber-400 text-sm font-semibold">
              <Flame className="w-4 h-4 fill-amber-400 text-amber-400 animate-pulse" />
              <span>5 ngày</span>
            </div>

            <div className="w-9 h-9 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center font-bold text-sm text-indigo-300">
              LD
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
        {/* Hero Banner */}
        <section className="relative overflow-hidden rounded-2xl border border-indigo-500/30 bg-gradient-to-br from-indigo-950/80 via-slate-900/90 to-purple-950/50 p-6 sm:p-10 shadow-2xl">
          <div className="absolute top-0 right-0 -mt-12 -mr-12 w-96 h-96 bg-indigo-500/15 rounded-full blur-3xl pointer-events-none" />
          <div className="relative z-10 max-w-3xl space-y-4">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-indigo-500/20 border border-indigo-500/40 text-indigo-300 text-xs font-semibold">
              <ShieldCheck className="w-3.5 h-3.5 text-indigo-400" />
              16/16 Modules Bắt buộc • Kiến trúc Monorepo Hoàn chỉnh
            </div>
            <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight text-white leading-tight">
              Làm chủ ngoại ngữ với thuật toán{' '}
              <span className="bg-gradient-to-r from-indigo-400 to-violet-300 bg-clip-text text-transparent">
                Spaced Repetition
              </span>{' '}
              thông minh
            </h1>
            <p className="text-slate-300 text-sm sm:text-base leading-relaxed">
              Học theo khóa học chuẩn A1, tự tạo bộ thẻ flashcards hai mặt, ôn tập ngắt quãng với 4 mức độ nhớ, hỗ trợ học ngoại tuyến PWA và tự động đồng bộ hai chiều.
            </p>
            <div className="pt-2 flex flex-wrap gap-3">
              <button
                onClick={() => setActiveTab('srs')}
                className="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm transition-all shadow-lg shadow-indigo-600/30 flex items-center gap-2"
              >
                <Play className="w-4 h-4 fill-white" />
                Ôn tập ngay (12 thẻ đến hạn)
              </button>
              <button
                onClick={() => setActiveTab('courses')}
                className="px-5 py-2.5 rounded-xl bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-slate-200 font-medium text-sm transition-all flex items-center gap-2"
              >
                Tiếp tục bài học gần nhất
                <ChevronRight className="w-4 h-4 text-slate-400" />
              </button>
            </div>
          </div>
        </section>

        {/* Quick Metrics */}
        <section className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="glass-card rounded-xl p-4 space-y-1">
            <span className="text-xs font-medium text-slate-400 flex items-center gap-1.5">
              <Clock className="w-3.5 h-3.5 text-indigo-400" />
              Thẻ cần ôn hôm nay
            </span>
            <div className="text-2xl font-bold text-white">12</div>
            <span className="text-[11px] text-indigo-400 font-medium">Lịch SRS v1</span>
          </div>

          <div className="glass-card rounded-xl p-4 space-y-1">
            <span className="text-xs font-medium text-slate-400 flex items-center gap-1.5">
              <BookOpen className="w-3.5 h-3.5 text-emerald-400" />
              Từ vựng đã thuộc
            </span>
            <div className="text-2xl font-bold text-white">48 / 60</div>
            <span className="text-[11px] text-emerald-400 font-medium">Khóa English A1</span>
          </div>

          <div className="glass-card rounded-xl p-4 space-y-1">
            <span className="text-xs font-medium text-slate-400 flex items-center gap-1.5">
              <Flame className="w-3.5 h-3.5 text-amber-400" />
              Chuỗi học liên tục
            </span>
            <div className="text-2xl font-bold text-white">5 ngày 🔥</div>
            <span className="text-[11px] text-amber-400 font-medium">Mục tiêu: 10 thẻ/ngày</span>
          </div>

          <div className="glass-card rounded-xl p-4 space-y-1">
            <span className="text-xs font-medium text-slate-400 flex items-center gap-1.5">
              <BarChart3 className="w-3.5 h-3.5 text-purple-400" />
              Tỷ lệ ghi nhớ
            </span>
            <div className="text-2xl font-bold text-white">91.4%</div>
            <span className="text-[11px] text-purple-400 font-medium">Theo thuật toán SRS</span>
          </div>
        </section>

        {/* Tab 1: Khóa học */}
        {activeTab === 'courses' && (
          <section className="space-y-6">
            <div className="flex justify-between items-center">
              <div>
                <h2 className="text-xl font-bold text-white">Khóa học Tiếng Anh A1 Căn bản</h2>
                <p className="text-xs text-slate-400">Cấu trúc Course → Unit → Lesson với từ vựng và bài tập Quiz</p>
              </div>
              <span className="px-2.5 py-1 rounded-md text-xs font-semibold bg-emerald-500/10 text-emerald-300 border border-emerald-500/30">
                Đã ghi danh
              </span>
            </div>

            <div className="grid md:grid-cols-3 gap-5">
              <div className="glass-card rounded-xl p-5 space-y-4 border-l-4 border-l-indigo-500">
                <div className="flex justify-between items-start">
                  <div>
                    <span className="text-xs font-semibold text-indigo-400">Unit 1 • Bài 1</span>
                    <h3 className="font-bold text-lg text-white mt-0.5">Greetings & Introductions</h3>
                  </div>
                  <CheckCircle2 className="w-5 h-5 text-emerald-400" />
                </div>
                <p className="text-xs text-slate-300">
                  Chào hỏi căn bản, tự giới thiệu tên, tuổi, quốc tịch và các mẫu câu lịch sự.
                </p>
                <div className="flex items-center justify-between text-xs text-slate-400 pt-2 border-t border-slate-800">
                  <span>10 Từ vựng • 1 Quiz</span>
                  <span className="text-emerald-400 font-medium">Hoàn thành 100%</span>
                </div>
              </div>

              <div className="glass-card rounded-xl p-5 space-y-4 border-l-4 border-l-indigo-500">
                <div className="flex justify-between items-start">
                  <div>
                    <span className="text-xs font-semibold text-indigo-400">Unit 1 • Bài 2</span>
                    <h3 className="font-bold text-lg text-white mt-0.5">Family & Relationships</h3>
                  </div>
                  <span className="px-2 py-0.5 rounded text-[11px] font-medium bg-amber-500/10 text-amber-300 border border-amber-500/30">
                    Đang học
                  </span>
                </div>
                <p className="text-xs text-slate-300">
                  Từ vựng về các thành viên trong gia đình, nghề nghiệp và miêu tả ngoại hình.
                </p>
                <div className="flex items-center justify-between text-xs text-slate-400 pt-2 border-t border-slate-800">
                  <span>12 Từ vựng • 1 Quiz</span>
                  <span className="text-amber-400 font-medium">Tiến độ 60%</span>
                </div>
              </div>

              <div className="glass-card rounded-xl p-5 space-y-4 border-l-4 border-l-slate-600 opacity-80">
                <div className="flex justify-between items-start">
                  <div>
                    <span className="text-xs font-semibold text-slate-400">Unit 1 • Bài 3</span>
                    <h3 className="font-bold text-lg text-white mt-0.5">Daily Routines</h3>
                  </div>
                  <span className="text-xs text-slate-500">Chưa bắt đầu</span>
                </div>
                <p className="text-xs text-slate-400">
                  Các hoạt động hằng ngày, cách nói giờ giấc, thói quen sinh hoạt và trạng từ tần suất.
                </p>
                <div className="flex items-center justify-between text-xs text-slate-400 pt-2 border-t border-slate-800">
                  <span>15 Từ vựng • 1 Quiz</span>
                  <button className="text-indigo-400 hover:text-indigo-300 font-medium flex items-center gap-1">
                    Bắt đầu học <ChevronRight className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            </div>
          </section>
        )}

        {/* Tab 2: Ôn tập Flashcards & SRS */}
        {activeTab === 'srs' && (
          <section className="space-y-6 max-w-2xl mx-auto">
            <div className="text-center space-y-1">
              <span className="text-xs font-semibold uppercase tracking-wider text-indigo-400">
                Phiên ôn tập Spaced Repetition (SRS)
              </span>
              <h2 className="text-2xl font-bold text-white">Thẻ 3 / 12 (Đến hạn hôm nay)</h2>
            </div>

            {/* Flashcard Component */}
            <div
              onClick={() => setIsFlipped(!isFlipped)}
              className="relative w-full h-80 rounded-2xl cursor-pointer perspective-1000 transition-all duration-300 select-none"
            >
              <div
                className={`w-full h-full rounded-2xl p-8 flex flex-col justify-between transition-all duration-500 transform glass-panel border ${
                  isFlipped ? 'border-indigo-500/60 bg-indigo-950/40' : 'border-slate-800 bg-slate-900/80 hover:border-slate-700'
                }`}
              >
                <div className="flex justify-between items-center text-xs text-slate-400">
                  <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-300 font-mono">
                    Bộ: Tiếng Anh A1 Căn bản
                  </span>
                  <span className="flex items-center gap-1 text-indigo-300">
                    <RotateCw className="w-3.5 h-3.5" /> Nhấp để lật thẻ
                  </span>
                </div>

                <div className="text-center space-y-3">
                  {!isFlipped ? (
                    <>
                      <span className="text-xs font-semibold text-indigo-400 uppercase tracking-widest">
                        Mặt trước (Từ vựng)
                      </span>
                      <h3 className="text-4xl font-extrabold text-white tracking-tight">Perseverance</h3>
                      <p className="text-sm font-mono text-slate-400">/ˌpɜː.sɪˈvɪə.rəns/</p>
                    </>
                  ) : (
                    <>
                      <span className="text-xs font-semibold text-emerald-400 uppercase tracking-widest">
                        Mặt sau (Ý nghĩa & Ví dụ)
                      </span>
                      <h3 className="text-2xl font-bold text-white">Sự kiên trì, bền chí</h3>
                      <p className="text-sm text-slate-300 italic max-w-md mx-auto">
                        &quot;Through perseverance and hard work, he achieved fluency in the language.&quot;
                      </p>
                    </>
                  )}
                </div>

                <div className="flex justify-center items-center gap-2 text-xs text-slate-400">
                  <Volume2 className="w-4 h-4 text-indigo-400 cursor-pointer hover:scale-110 transition-transform" />
                  <span>Âm thanh chuẩn UK/US</span>
                </div>
              </div>
            </div>

            {/* SRS Rating Buttons (Chỉ hiện khi đã lật mặt sau) */}
            {isFlipped && (
              <div className="space-y-3 pt-2 animate-in fade-in duration-300">
                <span className="text-xs text-slate-400 block text-center font-medium">
                  Đánh giá mức độ nhớ của bạn (Thuật toán SRS tính ngày ôn tiếp theo):
                </span>
                <div className="grid grid-cols-4 gap-3">
                  <button
                    onClick={() => setSelectedRating('Again')}
                    className={`py-3 px-2 rounded-xl border text-center transition-all ${
                      selectedRating === 'Again'
                        ? 'bg-rose-600 text-white border-rose-500 scale-105'
                        : 'bg-rose-950/30 hover:bg-rose-900/40 border-rose-900/50 text-rose-300'
                    }`}
                  >
                    <span className="block font-bold text-sm">Again</span>
                    <span className="text-[10px] text-rose-400/80">&lt; 10 phút</span>
                  </button>

                  <button
                    onClick={() => setSelectedRating('Hard')}
                    className={`py-3 px-2 rounded-xl border text-center transition-all ${
                      selectedRating === 'Hard'
                        ? 'bg-amber-600 text-white border-amber-500 scale-105'
                        : 'bg-amber-950/30 hover:bg-amber-900/40 border-amber-900/50 text-amber-300'
                    }`}
                  >
                    <span className="block font-bold text-sm">Hard</span>
                    <span className="text-[10px] text-amber-400/80">1 ngày</span>
                  </button>

                  <button
                    onClick={() => setSelectedRating('Good')}
                    className={`py-3 px-2 rounded-xl border text-center transition-all ${
                      selectedRating === 'Good'
                        ? 'bg-emerald-600 text-white border-emerald-500 scale-105'
                        : 'bg-emerald-950/30 hover:bg-emerald-900/40 border-emerald-900/50 text-emerald-300'
                    }`}
                  >
                    <span className="block font-bold text-sm">Good</span>
                    <span className="text-[10px] text-emerald-400/80">3 ngày</span>
                  </button>

                  <button
                    onClick={() => setSelectedRating('Easy')}
                    className={`py-3 px-2 rounded-xl border text-center transition-all ${
                      selectedRating === 'Easy'
                        ? 'bg-indigo-600 text-white border-indigo-500 scale-105'
                        : 'bg-indigo-950/30 hover:bg-indigo-900/40 border-indigo-900/50 text-indigo-300'
                    }`}
                  >
                    <span className="block font-bold text-sm">Easy</span>
                    <span className="text-[10px] text-indigo-400/80">7 ngày</span>
                  </button>
                </div>
              </div>
            )}
          </section>
        )}

        {/* Tab 3: Thống kê & Heatmap */}
        {activeTab === 'stats' && (
          <section className="space-y-6">
            <div>
              <h2 className="text-xl font-bold text-white">Thống kê Hoạt động & Ma trận Học tập</h2>
              <p className="text-xs text-slate-400">Theo dõi số lượt ôn tập hàng ngày, tỷ lệ nhớ và chuỗi Streak</p>
            </div>

            <div className="glass-panel rounded-2xl p-6 space-y-6">
              <div className="flex justify-between items-center">
                <span className="text-sm font-semibold text-slate-200">
                  Heatmap Hoạt động (30 ngày gần nhất)
                </span>
                <span className="text-xs text-slate-400">Mục tiêu: Đạt chuẩn hằng ngày</span>
              </div>

              {/* Heatmap Grid Demo */}
              <div className="grid grid-cols-10 sm:grid-cols-15 gap-2">
                {Array.from({ length: 30 }).map((_, index) => {
                  const intensity = (index * 7) % 5;
                  const bgColors = [
                    'bg-slate-800',
                    'bg-emerald-900/60',
                    'bg-emerald-700/70',
                    'bg-emerald-500',
                    'bg-emerald-400'
                  ];
                  return (
                    <div
                      key={index}
                      className={`h-7 rounded-md ${bgColors[intensity]} border border-slate-700/40 hover:scale-110 transition-transform cursor-pointer`}
                      title={`Ngày ${index + 1}: ${intensity * 4} lượt ôn tập`}
                    />
                  );
                })}
              </div>

              <div className="flex items-center justify-between text-xs text-slate-400 pt-4 border-t border-slate-800">
                <div className="flex items-center gap-1.5">
                  <span>Ít hơn</span>
                  <div className="w-3 h-3 rounded bg-slate-800" />
                  <div className="w-3 h-3 rounded bg-emerald-900/60" />
                  <div className="w-3 h-3 rounded bg-emerald-700/70" />
                  <div className="w-3 h-3 rounded bg-emerald-500" />
                  <span>Nhiều hơn</span>
                </div>
                <span>Múi giờ: Asia/Ho_Chi_Minh (UTC+7)</span>
              </div>
            </div>
          </section>
        )}

        {/* Tab 4: Offline & Sync */}
        {activeTab === 'offline' && (
          <section className="space-y-6 max-w-3xl mx-auto">
            <div>
              <h2 className="text-xl font-bold text-white">Trung tâm Quản trị Ngoại tuyến & Đồng bộ</h2>
              <p className="text-xs text-slate-400">Kiểm thử tính năng PWA, lưu trữ IndexedDB và giải quyết xung đột</p>
            </div>

            <div className="glass-panel rounded-2xl p-6 space-y-6">
              <div className="flex items-center justify-between p-4 rounded-xl bg-slate-800/60 border border-slate-700">
                <div className="flex items-center gap-3">
                  <div className={`w-3 h-3 rounded-full ${isOnline ? 'bg-emerald-400 animate-ping' : 'bg-amber-400'}`} />
                  <div>
                    <h4 className="text-sm font-semibold text-white">
                      Trạng thái mạng: {isOnline ? 'Đang kết nối Internet' : 'Đang ngắt kết nối (Offline)'}
                    </h4>
                    <p className="text-xs text-slate-400">
                      {isOnline
                        ? 'Tất cả sự kiện ôn tập sẽ gửi trực tiếp về Server ASP.NET Core.'
                        : 'Sự kiện ôn tập đang được xếp vào IndexedDB cục bộ với UUID idempotent.'}
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => setIsOnline(!isOnline)}
                  className="px-3 py-1.5 rounded-lg text-xs font-medium bg-indigo-600 hover:bg-indigo-500 text-white transition-all"
                >
                  {isOnline ? 'Mô phỏng mất mạng' : 'Mô phỏng có mạng'}
                </button>
              </div>

              <div className="space-y-3">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                  Hàng đợi đồng bộ cục bộ (IndexedDB Queue)
                </h4>
                <div className="space-y-2">
                  <div className="p-3 rounded-lg bg-slate-900/60 border border-slate-800 flex justify-between items-center text-xs">
                    <div>
                      <span className="font-mono text-indigo-400">[Review.Submit]</span>{' '}
                      <span className="text-slate-200">Thẻ #bd19a84e - Đánh giá: Good</span>
                    </div>
                    <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 font-medium">
                      Đã đồng bộ
                    </span>
                  </div>

                  <div className="p-3 rounded-lg bg-slate-900/60 border border-slate-800 flex justify-between items-center text-xs">
                    <div>
                      <span className="font-mono text-indigo-400">[Review.Submit]</span>{' '}
                      <span className="text-slate-200">Thẻ #f01a79e4 - Đánh giá: Again</span>
                    </div>
                    <span className="px-2 py-0.5 rounded bg-amber-500/10 text-amber-400 font-medium">
                      Chờ đồng bộ (Pending)
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </section>
        )}
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-800/80 bg-slate-950 py-6 text-center text-xs text-slate-500 space-y-2">
        <p>
          Dự án Mã nguồn mở: <strong className="text-slate-400">Language Learning Web Application</strong> — Đề tài môn PTPMMNM 2026.
        </p>
        <p>
          Quy chuẩn kỹ thuật: React 18+ • TypeScript • Tailwind CSS • ASP.NET Core 8 Web API • MySQL 8 • Clean Architecture • PWA Offline.
        </p>
      </footer>
    </div>
  );
}
export default App;
