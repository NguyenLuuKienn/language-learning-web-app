import { useLayoutEffect, useRef, useState } from 'react';

interface Line { word: string; x1: number; y1: number; x2: number; y2: number }
interface Props {
  words: string[];
  meanings: string[];
  matches: Record<string, string>;
  selectedWord: string | null;
  expected?: string;
  disabled: boolean;
  onSelectWord: (word: string) => void;
  onMatch: (word: string, meaning: string) => void;
  language: 'vi' | 'en';
}

export function MatchingBoard({ words, meanings, matches, selectedWord, expected, disabled, onSelectWord, onMatch, language }: Props) {
  const boardRef = useRef<HTMLDivElement>(null);
  const wordRefs = useRef(new Map<string, HTMLButtonElement>());
  const meaningRefs = useRef(new Map<string, HTMLButtonElement>());
  const [lines, setLines] = useState<Line[]>([]);
  const [size, setSize] = useState({ width: 1, height: 1 });

  useLayoutEffect(() => {
    const board = boardRef.current;
    if (!board) return;
    const measure = () => {
      const bounds = board.getBoundingClientRect();
      setSize({ width: bounds.width, height: bounds.height });
      setLines(Object.entries(matches).flatMap(([word, meaning]) => {
        const left = wordRefs.current.get(word)?.getBoundingClientRect();
        const right = meaningRefs.current.get(meaning)?.getBoundingClientRect();
        return left && right ? [{ word, x1: left.right - bounds.left, y1: left.top + left.height / 2 - bounds.top,
          x2: right.left - bounds.left, y2: right.top + right.height / 2 - bounds.top }] : [];
      }));
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(board);
    for (const element of wordRefs.current.values()) observer.observe(element);
    for (const element of meaningRefs.current.values()) observer.observe(element);
    return () => observer.disconnect();
  }, [words, meanings, matches]);

  const correct = new Map(expected?.split('; ').map((pair) => {
    const separator = pair.indexOf(' → ');
    return [pair.slice(0, separator), pair.slice(separator + 3)] as const;
  }) ?? []);

  return <div className="mt-5">
    <p className="mb-3 text-xs text-stone-600">{language === 'vi' ? 'Chọn từ bên trái rồi chọn nghĩa bên phải để nối. Chọn lại từ để đổi cặp.' : 'Select a word on the left, then its meaning on the right. Select again to change a pair.'}</p>
    <div ref={boardRef} className="relative grid grid-cols-2 gap-x-8 sm:gap-x-20">
      <svg className="pointer-events-none absolute inset-0 h-full w-full" viewBox={`0 0 ${size.width} ${size.height}`} aria-hidden="true">
        {lines.map((line) => <line key={line.word} x1={line.x1} y1={line.y1} x2={line.x2} y2={line.y2}
          stroke={expected ? (correct.get(line.word) === matches[line.word] ? '#059669' : '#e11d48') : '#0f766e'} strokeWidth="3" strokeLinecap="round" />)}
      </svg>
      <div className="space-y-2">{words.map((word) => <button key={word} type="button" ref={(element) => { if (element) wordRefs.current.set(word, element); else wordRefs.current.delete(word); }}
        disabled={disabled} onClick={() => onSelectWord(word)} className={`min-h-14 w-full rounded-xl border-2 px-3 py-2 text-sm font-bold ${selectedWord === word ? 'border-amber-500 bg-amber-50' : matches[word] ? 'border-emerald-500 bg-emerald-50' : 'border-stone-300 bg-[#FAF6EE]'}`}>{word}</button>)}</div>
      <div className="space-y-2">{meanings.map((meaning) => <button key={meaning} type="button" ref={(element) => { if (element) meaningRefs.current.set(meaning, element); else meaningRefs.current.delete(meaning); }}
        disabled={disabled || !selectedWord} onClick={() => { if (selectedWord) onMatch(selectedWord, meaning); }} className={`min-h-14 w-full rounded-xl border-2 px-3 py-2 text-sm ${Object.values(matches).includes(meaning) ? 'border-emerald-500 bg-emerald-50' : 'border-stone-300 bg-white'}`}>{meaning}</button>)}</div>
    </div>
  </div>;
}
