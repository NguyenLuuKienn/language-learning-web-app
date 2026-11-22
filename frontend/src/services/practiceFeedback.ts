let audioContext: AudioContext | null = null;

export function playPracticeFeedback(correct: boolean): void {
  try {
    audioContext ??= new AudioContext();
    if (audioContext.state === 'suspended') void audioContext.resume();
    const now = audioContext.currentTime;
    const tone = audioContext.createOscillator();
    const volume = audioContext.createGain();
    tone.type = 'sine';
    tone.frequency.setValueAtTime(correct ? 660 : 260, now);
    if (correct) tone.frequency.setValueAtTime(880, now + 0.1);
    volume.gain.setValueAtTime(0.06, now);
    volume.gain.exponentialRampToValueAtTime(0.001, now + 0.24);
    tone.connect(volume).connect(audioContext.destination);
    tone.start(now);
    tone.stop(now + 0.25);
  } catch { /* Browsers without audio support still show visual feedback. */ }
}

export function playPracticeConfirmation(): void {
  try {
    audioContext ??= new AudioContext();
    if (audioContext.state === 'suspended') void audioContext.resume();
    const now = audioContext.currentTime;
    const tone = audioContext.createOscillator();
    const volume = audioContext.createGain();
    tone.frequency.setValueAtTime(520, now);
    volume.gain.setValueAtTime(0.045, now);
    volume.gain.exponentialRampToValueAtTime(0.001, now + 0.14);
    tone.connect(volume).connect(audioContext.destination);
    tone.start(now); tone.stop(now + 0.15);
  } catch { /* Visual confirmation remains available. */ }
}
