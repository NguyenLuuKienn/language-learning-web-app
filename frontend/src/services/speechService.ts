// Service quản lý giọng đọc bản xứ đa ngôn ngữ (TTS - Text To Speech)
// Hỗ trợ: 🇬🇧 Tiếng Anh (Anh-Anh, Anh-Mỹ), 🇯🇵 Tiếng Nhật, 🇨🇳 Tiếng Trung, 🇰🇷 Tiếng Hàn, 🇻🇳 Tiếng Việt

export type SupportedLanguageCode = 'en' | 'ja' | 'zh' | 'ko' | 'vi';

export interface SpeechOptions {
  rate?: number;       // Tốc độ: 0.5 -> 1.5 (mặc định 0.95 cho người học)
  pitch?: number;      // Cao độ giọng: 0.8 -> 1.2
  accent?: 'us' | 'uk';// Cho tiếng Anh: US hoặc UK
  onStart?: () => void;
  onEnd?: () => void;
  onError?: (err: any) => void;
}

class SpeechService {
  private voices: SpeechSynthesisVoice[] = [];

  constructor() {
    this.initVoices();
  }

  private initVoices(): void {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
      return;
    }

    const loadVoices = () => {
      this.voices = window.speechSynthesis.getVoices();
    };

    loadVoices();
    if (window.speechSynthesis.onvoiceschanged !== undefined) {
      window.speechSynthesis.onvoiceschanged = loadVoices;
    }
  }

  /**
   * Lấy danh sách giọng đọc tốt nhất cho ngôn ngữ được chỉ định
   */
  public getBestVoice(langCode: string, accent: 'us' | 'uk' = 'us'): SpeechSynthesisVoice | null {
    if (!this.voices.length) {
      this.voices = window.speechSynthesis.getVoices();
    }

    const code = langCode.toLowerCase();

    // 1. Tiếng Anh
    if (code.startsWith('en')) {
      const targetLocale = accent === 'uk' ? 'en-gb' : 'en-us';
      // Ưu tiên Natural / Google / Premium voices
      const matched = this.voices.find(
        (v) =>
          v.lang.toLowerCase().replace('_', '-').startsWith(targetLocale) &&
          (v.name.includes('Natural') || v.name.includes('Google') || v.name.includes('Premium'))
      );
      if (matched) return matched;

      // Fallback giọng bất kỳ đúng locale
      const anyLocale = this.voices.find((v) =>
        v.lang.toLowerCase().replace('_', '-').startsWith(targetLocale)
      );
      if (anyLocale) return anyLocale;

      // Fallback giọng tiếng Anh bất kỳ
      return this.voices.find((v) => v.lang.toLowerCase().startsWith('en')) || null;
    }

    // 2. Tiếng Nhật
    if (code === 'ja' || code === 'jp') {
      const matched = this.voices.find(
        (v) =>
          v.lang.toLowerCase().startsWith('ja') &&
          (v.name.includes('Google') || v.name.includes('Natural') || v.name.includes('Kyoko') || v.name.includes('Otoya'))
      );
      if (matched) return matched;
      return this.voices.find((v) => v.lang.toLowerCase().startsWith('ja')) || null;
    }

    // 3. Tiếng Trung
    if (code === 'zh' || code === 'cn') {
      const matched = this.voices.find(
        (v) =>
          (v.lang.toLowerCase().startsWith('zh') || v.lang.toLowerCase().startsWith('cmn')) &&
          (v.name.includes('Google') || v.name.includes('Natural') || v.name.includes('Xiaoxiao') || v.name.includes('Yunxi'))
      );
      if (matched) return matched;
      return this.voices.find((v) => v.lang.toLowerCase().startsWith('zh')) || null;
    }

    // 4. Tiếng Hàn
    if (code === 'ko' || code === 'kr') {
      const matched = this.voices.find(
        (v) =>
          v.lang.toLowerCase().startsWith('ko') &&
          (v.name.includes('Google') || v.name.includes('Natural') || v.name.includes('SunHi') || v.name.includes('Yuna'))
      );
      if (matched) return matched;
      return this.voices.find((v) => v.lang.toLowerCase().startsWith('ko')) || null;
    }

    // 5. Tiếng Việt
    if (code === 'vi' || code === 'vn') {
      return this.voices.find((v) => v.lang.toLowerCase().startsWith('vi')) || null;
    }

    return null;
  }

  /**
   * Phát âm văn bản chuẩn xác theo ngôn ngữ
   */
  public speak(text: string, langCode: string = 'en', options: SpeechOptions = {}): boolean {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
      console.warn('SpeechSynthesis is not supported on this device/browser.');
      return false;
    }

    // Làm sạch chuỗi trước khi đọc (bỏ dấu ngoặc phiên âm, ký tự đặc biệt)
    const cleanText = text
      .replace(/\(.*?\)/g, '')
      .replace(/\[.*?\]/g, '')
      .replace(/[\/]/g, '')
      .trim();

    if (!cleanText) return false;

    // Hủy bỏ âm thanh đang phát trước đó để không bị dính chữ
    window.speechSynthesis.cancel();

    const utterance = new SpeechSynthesisUtterance(cleanText);
    utterance.rate = options.rate ?? 0.95; // Tốc độ vừa phải cho người học
    utterance.pitch = options.pitch ?? 1.0;

    const voice = this.getBestVoice(langCode, options.accent);
    if (voice) {
      utterance.voice = voice;
      utterance.lang = voice.lang;
    } else {
      // Thiết lập fallback lang tag chuẩn
      const code = langCode.toLowerCase();
      if (code === 'ja') utterance.lang = 'ja-JP';
      else if (code === 'zh') utterance.lang = 'zh-CN';
      else if (code === 'ko') utterance.lang = 'ko-KR';
      else if (code === 'vi') utterance.lang = 'vi-VN';
      else utterance.lang = options.accent === 'uk' ? 'en-GB' : 'en-US';
    }

    if (options.onStart) utterance.onstart = options.onStart;
    if (options.onEnd) utterance.onend = options.onEnd;
    if (options.onError) utterance.onerror = options.onError;

    window.speechSynthesis.speak(utterance);
    return true;
  }

  /**
   * Dừng phát âm
   */
  public stop(): void {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  }

  /**
   * Kiểm tra xem đang phát âm không
   */
  public isSpeaking(): boolean {
    return typeof window !== 'undefined' && 'speechSynthesis' in window && window.speechSynthesis.speaking;
  }
}

export const speechService = new SpeechService();
