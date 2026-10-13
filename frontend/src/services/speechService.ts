export type SupportedLanguageCode = 'ja' | 'en' | 'zh' | 'ko';

const languageLocaleMap: Record<SupportedLanguageCode, string> = {
  ja: 'ja-JP',
  en: 'en-US',
  zh: 'zh-CN',
  ko: 'ko-KR',
};

export function speakText(
  text: string,
  lang: SupportedLanguageCode = 'ja',
  rate: number = 0.9
): void {
  if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
    return;
  }

  // Cancel any ongoing speech playback
  window.speechSynthesis.cancel();

  const utterance = new SpeechSynthesisUtterance(text);
  const targetLocale = languageLocaleMap[lang] || 'ja-JP';
  utterance.lang = targetLocale;
  utterance.rate = rate;

  const voices = window.speechSynthesis.getVoices();
  const matchedVoice = voices.find(
    (voice) => voice.lang.toLowerCase() === targetLocale.toLowerCase() || voice.lang.startsWith(lang)
  );

  if (matchedVoice) {
    utterance.voice = matchedVoice;
  }

  window.speechSynthesis.speak(utterance);
}
