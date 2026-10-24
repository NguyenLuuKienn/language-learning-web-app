
interface FlashcardProps {
  front: string;
  back: string;
  isFlipped: boolean;
  onFlip: () => void;
}

export function Flashcard({
  front,
  back,
  isFlipped,
  onFlip,
}: FlashcardProps) {
  return (
    <div
      className="h-[320px] w-full"
      style={{ perspective: '1000px' }}
    >
      <button
        type="button"
        onClick={onFlip}
        aria-label={isFlipped ? 'Lật về mặt trước' : 'Lật sang mặt sau'}
        aria-pressed={isFlipped}
        className="block h-full w-full rounded-xl text-left focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-rose-600"
      >
        <div
          className="relative h-full w-full transition-transform duration-500"
          style={{
            transformStyle: 'preserve-3d',
            transform: isFlipped
              ? 'rotateY(180deg)'
              : 'rotateY(0deg)',
          }}
        >
          <div
            className="flashcard-yellow-grid absolute inset-0 flex flex-col items-center justify-center rounded-xl border-notebook-2 p-8 text-center"
            style={{ backfaceVisibility: 'hidden' }}
          >
            <span className="mb-4 text-xs font-bold text-stone-500">
              MẶT TRƯỚC
            </span>

            <h2 className="max-w-full break-words text-3xl font-bold text-stone-900">
              {front}
            </h2>

            <p className="absolute bottom-5 text-xs text-stone-500">
              Click để lật thẻ
            </p>
          </div>

          <div
            className="absolute inset-0 flex flex-col items-center justify-center rounded-xl border-notebook-2 bg-white p-8 text-center"
            style={{
              backfaceVisibility: 'hidden',
              transform: 'rotateY(180deg)',
            }}
          >
            <span className="mb-4 text-xs font-bold text-stone-500">
              MẶT SAU
            </span>

            <h2 className="max-w-full break-words text-3xl font-bold text-stone-900">
              {back}
            </h2>

            <p className="absolute bottom-5 text-xs text-stone-500">
              Click để lật lại
            </p>
          </div>
        </div>
      </button>
    </div>
  );
}
