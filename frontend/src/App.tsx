import { useState, useEffect } from 'react';
import {
  BookOutlined,
  AppstoreOutlined,
  BarChartOutlined,
  SyncOutlined,
  FireFilled,
  SoundOutlined,
  CheckCircleFilled,
  SwapOutlined,
  WifiOutlined,
  DisconnectOutlined,
  RightOutlined,
  EditOutlined
} from '@ant-design/icons';

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
    <div className="min-h-screen paper-bg text-stone-800 flex flex-col font-sans">
      {/* Top Banner Thanh Lịch */}
      <div className="bg-[#F2ECE1] border-b border-[#E5DEC9] px-6 py-2 text-xs flex justify-between items-center text-stone-600">
        <div className="flex items-center gap-3">
          <span className="font-semibold text-stone-700 tracking-wide uppercase text-[11px]">
            Sổ tay học ngoại ngữ • LinguaNote
          </span>
          <span className="hidden sm:inline text-stone-400">|</span>
          <span className="hidden sm:inline text-stone-500">
            Phương pháp Spaced Repetition ghi nhớ tự nhiên
          </span>
        </div>

        <button
          onClick={() => setIsOnline(!isOnline)}
          className="flex items-center gap-2 px-2.5 py-1 rounded bg-stone-50 hover:bg-white border border-stone-300 text-xs text-stone-700 transition shadow-2xs"
          title="Nhấp để mô phỏng chuyển đổi Trực tuyến / Ngoại tuyến"
        >
          {isOnline ? (
            <>
              <WifiOutlined className="text-emerald-600" />
              <span>Trực tuyến</span>
            </>
          ) : (
            <>
              <DisconnectOutlined className="text-amber-600" />
              <span className="text-amber-800 font-medium">Ngoại tuyến (2 ghi chú chờ sync)</span>
            </>
          )}
        </button>
      </div>

      {/* Header Giao Diện Nền Giấy Sổ Tay */}
      <header className="bg-white/90 backdrop-blur-sm border-b border-[#E8E2D5] sticky top-0 z-50 shadow-2xs">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          {/* Logo & Tên Dự Án */}
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-[#EFE9DC] border border-[#DDD4C3] flex items-center justify-center text-stone-700">
              <BookOutlined className="text-lg" />
            </div>
            <div>
              <span className="font-serif-title font-bold text-xl tracking-tight text-stone-900 block leading-tight">
                LinguaNote
              </span>
              <span className="text-[10px] tracking-wider uppercase text-stone-500 block font-sans">
                Flashcards & SRS
              </span>
            </div>
          </div>

          {/* Navigation Tabs Tinh Gọn */}
          <nav className="flex items-center gap-1 bg-[#F4EFE6] p-1 rounded-lg border border-[#E5DEC9]">
            <button
              onClick={() => setActiveTab('courses')}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-all flex items-center gap-1.5 ${
                activeTab === 'courses'
                  ? 'bg-white text-stone-900 shadow-2xs font-semibold'
                  : 'text-stone-600 hover:text-stone-900'
              }`}
            >
              <BookOutlined />
              <span>Khóa học</span>
            </button>

            <button
              onClick={() => setActiveTab('srs')}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-all flex items-center gap-1.5 ${
                activeTab === 'srs'
                  ? 'bg-white text-stone-900 shadow-2xs font-semibold'
                  : 'text-stone-600 hover:text-stone-900'
              }`}
            >
              <AppstoreOutlined />
              <span>Bộ thẻ SRS</span>
            </button>

            <button
              onClick={() => setActiveTab('stats')}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-all flex items-center gap-1.5 ${
                activeTab === 'stats'
                  ? 'bg-white text-stone-900 shadow-2xs font-semibold'
                  : 'text-stone-600 hover:text-stone-900'
              }`}
            >
              <BarChartOutlined />
              <span>Nhật ký học</span>
            </button>

            <button
              onClick={() => setActiveTab('offline')}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-all flex items-center gap-1.5 ${
                activeTab === 'offline'
                  ? 'bg-white text-stone-900 shadow-2xs font-semibold'
                  : 'text-stone-600 hover:text-stone-900'
              }`}
            >
              <SyncOutlined />
              <span>Đồng bộ</span>
            </button>
          </nav>

          {/* Streak Badge */}
          <div className="flex items-center gap-2">
            <div className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-[#FFF8EB] border border-[#F3E2C2] text-amber-800 text-xs font-semibold shadow-2xs">
              <FireFilled className="text-amber-500 text-sm" />
              <span>5 ngày</span>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 max-w-5xl w-full mx-auto px-4 sm:px-6 py-8 space-y-8">
        {/* Banner Giới Thiệu Dạng Giấy Note Dán */}
        <section className="paper-card rounded-xl p-6 sm:p-8 relative overflow-hidden bg-[#FFFDF9] border border-[#EBE3D3]">
          <div className="washi-tape"></div>
          <div className="max-w-2xl space-y-3">
            <span className="text-xs uppercase tracking-widest font-semibold text-stone-500">
              Mục tiêu hôm nay: 15 từ mới • 12 thẻ ôn tập
            </span>
            <h1 className="text-2xl sm:text-3xl font-bold font-serif-title text-stone-900 leading-snug">
              Ôn luyện từ vựng bền bỉ theo chu kỳ khoa học
            </h1>
            <p className="text-stone-600 text-sm leading-relaxed">
              Mỗi từ vựng được ghi chú cẩn thận như trên trang giấy tay. Hệ thống Spaced Repetition sẽ tự động tính toán thời điểm vàng để bạn lật lại thẻ trước khi kịp quên.
            </p>
            <div className="pt-2 flex gap-3">
              <button
                onClick={() => setActiveTab('srs')}
                className="px-4 py-2 rounded-lg bg-stone-800 hover:bg-stone-900 text-stone-50 text-xs font-medium transition shadow-2xs flex items-center gap-2"
              >
                <span>Mở phiên ôn tập (12 thẻ)</span>
                <RightOutlined className="text-[10px]" />
              </button>
              <button
                onClick={() => setActiveTab('courses')}
                className="px-4 py-2 rounded-lg bg-[#F5EFE6] hover:bg-[#EFE8DD] text-stone-800 border border-[#DDD4C3] text-xs font-medium transition"
              >
                Học bài tiếp theo (Unit 1 - Lesson 2)
              </button>
            </div>
          </div>
        </section>

        {/* 4 Chỉ Số Tóm Tắt Dạng Thẻ Ghi Chú Mini */}
        <section className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="paper-card rounded-lg p-4 bg-white border border-[#E8E2D5]">
            <span className="text-xs text-stone-500 block mb-1">Thẻ đến hạn hôm nay</span>
            <div className="text-2xl font-bold text-stone-900 font-serif-title">12 thẻ</div>
            <span className="text-[11px] text-amber-700 bg-amber-50 px-1.5 py-0.5 rounded mt-2 inline-block">
              Cần ôn trước 23:59
            </span>
          </div>

          <div className="paper-card rounded-lg p-4 bg-white border border-[#E8E2D5]">
            <span className="text-xs text-stone-500 block mb-1">Từ vựng đã thuộc</span>
            <div className="text-2xl font-bold text-stone-900 font-serif-title">48 / 60</div>
            <span className="text-[11px] text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded mt-2 inline-block">
              Khóa Tiếng Anh A1
            </span>
          </div>

          <div className="paper-card rounded-lg p-4 bg-white border border-[#E8E2D5]">
            <span className="text-xs text-stone-500 block mb-1">Chuỗi ngày học</span>
            <div className="text-2xl font-bold text-stone-900 font-serif-title">5 ngày</div>
            <span className="text-[11px] text-orange-700 bg-orange-50 px-1.5 py-0.5 rounded mt-2 inline-block">
              Đều đặn mỗi ngày
            </span>
          </div>

          <div className="paper-card rounded-lg p-4 bg-white border border-[#E8E2D5]">
            <span className="text-xs text-stone-500 block mb-1">Tỷ lệ ghi nhớ</span>
            <div className="text-2xl font-bold text-stone-900 font-serif-title">91.4%</div>
            <span className="text-[11px] text-sky-700 bg-sky-50 px-1.5 py-0.5 rounded mt-2 inline-block">
              Đạt chuẩn SRS v1
            </span>
          </div>
        </section>

        {/* TAB 1: KHÓA HỌC DẠNG CUỐN VỞ GHI BÀI */}
        {activeTab === 'courses' && (
          <section className="space-y-4">
            <div className="flex justify-between items-center border-b border-[#E5DEC9] pb-3">
              <div>
                <h2 className="text-xl font-bold font-serif-title text-stone-900">
                  Khóa học: Tiếng Anh A1 Cơ Bản
                </h2>
                <p className="text-xs text-stone-500">Giáo trình 5 bài học nền tảng kèm bài tập thực hành</p>
              </div>
              <span className="text-xs font-semibold px-2.5 py-1 rounded bg-[#EBF5EE] text-emerald-800 border border-[#CDE5D4]">
                Đang học
              </span>
            </div>

            <div className="space-y-3">
              {/* Lesson 1 */}
              <div className="paper-card rounded-lg p-4 bg-white border-l-4 border-l-emerald-600 flex justify-between items-center">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-[11px] font-bold text-emerald-800 bg-emerald-50 px-2 py-0.5 rounded">
                      Bài 1
                    </span>
                    <h3 className="font-bold text-stone-900 text-sm">Greetings & Introductions</h3>
                  </div>
                  <p className="text-xs text-stone-500">
                    Từ vựng chào hỏi, tự giới thiệu tên, nghề nghiệp và đất nước.
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-emerald-700 font-medium flex items-center gap-1">
                    <CheckCircleFilled /> Hoàn thành
                  </span>
                  <button className="px-3 py-1 text-xs rounded border border-stone-300 hover:bg-stone-50 transition">
                    Xem lại
                  </button>
                </div>
              </div>

              {/* Lesson 2 */}
              <div className="paper-card rounded-lg p-4 bg-white border-l-4 border-l-amber-600 flex justify-between items-center">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-[11px] font-bold text-amber-800 bg-amber-50 px-2 py-0.5 rounded">
                      Bài 2
                    </span>
                    <h3 className="font-bold text-stone-900 text-sm">Family & Relationships</h3>
                  </div>
                  <p className="text-xs text-stone-500">
                    Thành viên gia đình, quan hệ bạn bè, đại từ nhân xưng.
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-amber-700 font-medium">Tiến độ: 60%</span>
                  <button className="px-3 py-1 text-xs rounded bg-stone-800 text-stone-50 hover:bg-stone-900 transition">
                    Học tiếp
                  </button>
                </div>
              </div>

              {/* Lesson 3 */}
              <div className="paper-card rounded-lg p-4 bg-[#FCFAF6] border-l-4 border-l-stone-300 opacity-90 flex justify-between items-center">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-[11px] font-bold text-stone-600 bg-stone-100 px-2 py-0.5 rounded">
                      Bài 3
                    </span>
                    <h3 className="font-bold text-stone-700 text-sm">Daily Routines & Hobbies</h3>
                  </div>
                  <p className="text-xs text-stone-400">
                    Hoạt động hằng ngày, thời gian biểu và sở thích cá nhân.
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-stone-400">Chưa bắt đầu</span>
                  <button className="px-3 py-1 text-xs rounded border border-stone-300 hover:bg-white transition text-stone-600">
                    Bắt đầu
                  </button>
                </div>
              </div>
            </div>
          </section>
        )}

        {/* TAB 2: FLASHCARD GIỐNG THẺ GHI CHÚ INDEX CARD BẰNG GIẤY THẬT */}
        {activeTab === 'srs' && (
          <section className="space-y-6 max-w-xl mx-auto">
            <div className="text-center space-y-1">
              <span className="text-xs font-semibold uppercase tracking-wider text-stone-500">
                Thẻ học ôn tập • Spaced Repetition
              </span>
              <h2 className="text-xl font-bold font-serif-title text-stone-900">
                Thẻ 3 / 12 (Đến hạn hôm nay)
              </h2>
            </div>

            {/* Tấm Thẻ Giấy Index Card Hai Mặt */}
            <div
              onClick={() => setIsFlipped(!isFlipped)}
              className="relative w-full h-72 rounded-xl cursor-pointer select-none transition-all duration-300 hover:shadow-md"
            >
              <div className="flashcard-note w-full h-full rounded-xl p-8 flex flex-col justify-between relative overflow-hidden">
                <div className="washi-tape"></div>

                <div className="flex justify-between items-center text-xs text-stone-400 border-b border-[#F0EAE1] pb-2">
                  <span className="font-mono text-stone-600">Deck: Từ vựng A1</span>
                  <span className="flex items-center gap-1 text-stone-500">
                    <SwapOutlined /> Nhấp vào thẻ để lật
                  </span>
                </div>

                <div className="text-center space-y-3 ruled-paper py-4">
                  {!isFlipped ? (
                    <>
                      <span className="text-[11px] uppercase tracking-widest text-stone-400 font-semibold block -mb-1">
                        Từ vựng
                      </span>
                      <h3 className="text-4xl font-serif-title font-bold text-stone-900">
                        Perseverance
                      </h3>
                      <p className="text-xs font-mono text-stone-500">/ˌpɜː.sɪˈvɪə.rəns/ (n)</p>
                    </>
                  ) : (
                    <>
                      <span className="text-[11px] uppercase tracking-widest text-emerald-700 font-semibold block -mb-1">
                        Ý nghĩa & Ví dụ
                      </span>
                      <h3 className="text-2xl font-serif-title font-bold text-stone-900">
                        Sự kiên trì, bền chí
                      </h3>
                      <p className="text-xs text-stone-600 italic max-w-sm mx-auto leading-relaxed">
                        &ldquo;Success comes through daily practice and perseverance.&rdquo;
                      </p>
                    </>
                  )}
                </div>

                <div className="flex justify-between items-center text-xs text-stone-400 border-t border-[#F0EAE1] pt-2">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      alert('Phát âm thanh: Perseverance');
                    }}
                    className="flex items-center gap-1.5 text-stone-600 hover:text-stone-900"
                  >
                    <SoundOutlined /> Phát âm (UK)
                  </button>
                  <span className="text-[11px] text-stone-400">Phím tắt: Space (Lật) • 1-4 (Chấm điểm)</span>
                </div>
              </div>
            </div>

            {/* 4 Nút Đánh Giá Mức Nhớ Chuẩn SRS (Giao Diện Note Tự Nhiên) */}
            {isFlipped && (
              <div className="space-y-3 pt-2">
                <span className="text-xs text-stone-500 block text-center">
                  Bạn ghi nhớ từ này như thế nào? (Chọn mức để lên lịch ôn):
                </span>
                <div className="grid grid-cols-4 gap-3">
                  <button
                    onClick={() => setSelectedRating('Again')}
                    className={`py-2.5 px-2 rounded-lg border text-center transition ${
                      selectedRating === 'Again'
                        ? 'bg-rose-100 border-rose-400 text-rose-900 font-bold'
                        : 'bg-white hover:bg-rose-50 border-stone-300 text-stone-800'
                    }`}
                  >
                    <span className="block text-xs font-semibold text-rose-700">Again</span>
                    <span className="text-[10px] text-stone-400">&lt; 10 phút</span>
                  </button>

                  <button
                    onClick={() => setSelectedRating('Hard')}
                    className={`py-2.5 px-2 rounded-lg border text-center transition ${
                      selectedRating === 'Hard'
                        ? 'bg-amber-100 border-amber-400 text-amber-900 font-bold'
                        : 'bg-white hover:bg-amber-50 border-stone-300 text-stone-800'
                    }`}
                  >
                    <span className="block text-xs font-semibold text-amber-700">Hard</span>
                    <span className="text-[10px] text-stone-400">1 ngày</span>
                  </button>

                  <button
                    onClick={() => setSelectedRating('Good')}
                    className={`py-2.5 px-2 rounded-lg border text-center transition ${
                      selectedRating === 'Good'
                        ? 'bg-emerald-100 border-emerald-400 text-emerald-900 font-bold'
                        : 'bg-white hover:bg-emerald-50 border-stone-300 text-stone-800'
                    }`}
                  >
                    <span className="block text-xs font-semibold text-emerald-700">Good</span>
                    <span className="text-[10px] text-stone-400">3 ngày</span>
                  </button>

                  <button
                    onClick={() => setSelectedRating('Easy')}
                    className={`py-2.5 px-2 rounded-lg border text-center transition ${
                      selectedRating === 'Easy'
                        ? 'bg-sky-100 border-sky-400 text-sky-900 font-bold'
                        : 'bg-white hover:bg-sky-50 border-stone-300 text-stone-800'
                    }`}
                  >
                    <span className="block text-xs font-semibold text-sky-700">Easy</span>
                    <span className="text-[10px] text-stone-400">7 ngày</span>
                  </button>
                </div>
              </div>
            )}
          </section>
        )}

        {/* TAB 3: NHẬT KÝ HỌC & HEATMAP */}
        {activeTab === 'stats' && (
          <section className="space-y-4">
            <div className="border-b border-[#E5DEC9] pb-3">
              <h2 className="text-xl font-bold font-serif-title text-stone-900">
                Nhật Ký Học Tập 30 Ngày Gần Nhất
              </h2>
              <p className="text-xs text-stone-500">
                Ghi nhận số lần lật thẻ và hoàn thành mục tiêu học hằng ngày
              </p>
            </div>

            <div className="paper-card rounded-xl p-6 bg-white border border-[#E8E2D5] space-y-4">
              <div className="flex justify-between items-center text-xs text-stone-600">
                <span className="font-semibold">Mức độ chăm chỉ trong tháng</span>
                <span>Múi giờ: Asia/Ho_Chi_Minh</span>
              </div>

              {/* Heatmap Ô Vuông Sổ Tay Màu Xanh Thảo Mộc */}
              <div className="grid grid-cols-10 sm:grid-cols-15 gap-2 pt-2">
                {Array.from({ length: 30 }).map((_, index) => {
                  const level = (index * 7) % 5;
                  const colors = [
                    'bg-[#F5F2EB] border-[#E8E2D5]',
                    'bg-[#E1EFE3] border-[#C2DEC7]',
                    'bg-[#B5DFC0] border-[#93CDA3]',
                    'bg-[#80C993] border-[#5EAE74]',
                    'bg-[#4FA966] border-[#388D4E]'
                  ];
                  return (
                    <div
                      key={index}
                      className={`h-7 rounded border ${colors[level]} hover:scale-105 transition-transform cursor-pointer`}
                      title={`Ngày ${index + 1}: ${level * 4} lượt ôn`}
                    />
                  );
                })}
              </div>

              <div className="flex items-center justify-between text-xs text-stone-400 pt-3 border-t border-[#F0EAE1]">
                <div className="flex items-center gap-1.5">
                  <span>Ít hơn</span>
                  <div className="w-3 h-3 rounded bg-[#F5F2EB] border border-[#E8E2D5]" />
                  <div className="w-3 h-3 rounded bg-[#E1EFE3]" />
                  <div className="w-3 h-3 rounded bg-[#B5DFC0]" />
                  <div className="w-3 h-3 rounded bg-[#4FA966]" />
                  <span>Nhiều hơn</span>
                </div>
                <span className="text-stone-500">Chuỗi kỷ lục: 14 ngày liên tiếp</span>
              </div>
            </div>
          </section>
        )}

        {/* TAB 4: OFFLINE & ĐỒNG BỘ */}
        {activeTab === 'offline' && (
          <section className="space-y-4 max-w-2xl mx-auto">
            <div className="border-b border-[#E5DEC9] pb-3">
              <h2 className="text-xl font-bold font-serif-title text-stone-900">
                Ghi Chú Ngoại Tuyến & Đồng Bộ
              </h2>
              <p className="text-xs text-stone-500">Hỗ trợ học khi mất kết nối mạng và đồng bộ tự động</p>
            </div>

            <div className="paper-card rounded-xl p-6 bg-white border border-[#E8E2D5] space-y-4">
              <div className="p-4 rounded-lg bg-[#FAF7F0] border border-[#EBE3D3] flex items-center justify-between">
                <div>
                  <h4 className="text-xs font-bold text-stone-800">
                    Trạng thái: {isOnline ? 'Đang kết nối Internet' : 'Đang ngắt mạng (Offline)'}
                  </h4>
                  <p className="text-xs text-stone-500 mt-0.5">
                    {isOnline
                      ? 'Dữ liệu được cập nhật tức thời về server.'
                      : 'Đang lưu tạm các kết quả ôn tập vào bộ nhớ máy (IndexedDB).'}
                  </p>
                </div>
                <button
                  onClick={() => setIsOnline(!isOnline)}
                  className="px-3 py-1.5 rounded text-xs bg-stone-800 hover:bg-stone-900 text-stone-50 transition"
                >
                  {isOnline ? 'Ngắt mạng thử' : 'Nối mạng lại'}
                </button>
              </div>

              <div className="space-y-2 pt-2">
                <span className="text-xs font-semibold text-stone-600 block">
                  Hàng đợi đồng bộ (Sync Queue)
                </span>
                <div className="p-3 rounded border border-[#EDE7DB] bg-white flex justify-between items-center text-xs">
                  <span className="text-stone-700">
                    <EditOutlined className="mr-1 text-stone-400" />
                    Ôn từ <strong>Perseverance</strong> (Đánh giá: Good)
                  </span>
                  <span className="text-[11px] text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded font-medium">
                    Đã đồng bộ
                  </span>
                </div>

                <div className="p-3 rounded border border-[#EDE7DB] bg-white flex justify-between items-center text-xs">
                  <span className="text-stone-700">
                    <EditOutlined className="mr-1 text-stone-400" />
                    Ôn từ <strong>Determination</strong> (Đánh giá: Again)
                  </span>
                  <span className="text-[11px] text-amber-700 bg-amber-50 px-2 py-0.5 rounded font-medium">
                    Chờ gửi lên server
                  </span>
                </div>
              </div>
            </div>
          </section>
        )}
      </main>

      {/* Footer Mộc Mạc */}
      <footer className="border-t border-[#E8E2D5] bg-[#F4EFE6] py-6 text-center text-xs text-stone-500 space-y-1">
        <p className="font-serif-title text-stone-700 font-semibold">
          LinguaNote — Sổ tay học ngoại ngữ & Flashcards
        </p>
        <p className="text-[11px] text-stone-400">
          Đề tài môn Phát triển phần mềm mã nguồn mở • React TypeScript & ASP.NET Core
        </p>
      </footer>
    </div>
  );
}
export default App;
