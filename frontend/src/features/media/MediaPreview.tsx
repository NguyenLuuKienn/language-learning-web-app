import { useEffect, useRef, useState } from 'react';
import { Volume2 } from 'lucide-react';

interface MediaPreviewProps {
  text: string;
  languageCode?: string;
  audioUrl?: string | null;
  imageUrl?: string | null;
}

export function MediaPreview({ text, languageCode, audioUrl, imageUrl }: MediaPreviewProps) {
  const activeAudio = useRef<HTMLAudioElement | null>(null);
  const [imageFailed, setImageFailed] = useState(false);
  const [message, setMessage] = useState('');

  useEffect(() => {
    return () => {
      activeAudio.current?.pause();
      window.speechSynthesis?.cancel();
    };
  }, [audioUrl, imageUrl, text]);

  function fallback() {
    activeAudio.current?.pause();
    if (!('speechSynthesis' in window)) {
      setMessage('Không thể phát âm thanh trên trình duyệt này.');
      return;
    }
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    if (languageCode) utterance.lang = languageCode;
    utterance.onerror = () => setMessage('Không thể phát âm thanh.');
    window.speechSynthesis.speak(utterance);
    setMessage('Đang phát âm bằng giọng đọc của trình duyệt.');
  }

  function play() {
    setMessage('');
    if (activeAudio.current) {
      activeAudio.current.onerror = null;
      activeAudio.current.pause();
    }
    if (!audioUrl) {
      fallback();
      return;
    }
    const audio = new Audio(audioUrl);
    activeAudio.current = audio;
    let fallbackStarted = false;
    const fallbackOnce = () => {
      if (fallbackStarted) return;
      fallbackStarted = true;
      fallback();
    };
    audio.onerror = fallbackOnce;
    void audio.play().catch(fallbackOnce);
  }

  return (
    <div className="flex flex-col items-center gap-2">
      {imageUrl && !imageFailed && <img src={imageUrl} alt={`Minh họa ${text}`} loading="lazy" onError={() => setImageFailed(true)} className="max-h-44 rounded-lg object-contain" />}
      {imageFailed && <p role="status" className="text-xs text-stone-500">Không tải được ảnh minh họa.</p>}
      <button type="button" onClick={play} disabled={!audioUrl && !('speechSynthesis' in window)} aria-label={`Nghe phát âm ${text}`} className="rounded-lg border-2 border-stone-900 bg-amber-100 p-2 hover:bg-amber-200 disabled:opacity-50"><Volume2 size={19} aria-hidden="true" /></button>
      {message && <p role="status" className="text-xs text-stone-600">{message}</p>}
    </div>
  );
}
