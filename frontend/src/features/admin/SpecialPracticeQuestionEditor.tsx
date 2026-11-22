type Entry = Record<string, unknown>;
type Kind = 'certificate' | 'interview';

interface Props {
  kind: Kind;
  value: string;
  onChange: (value: string) => void;
  vi: boolean;
}

function parse(value: string): Entry[] | null {
  try {
    const result: unknown = JSON.parse(value);
    return Array.isArray(result) && result.every(item => item && typeof item === 'object' && !Array.isArray(item))
      ? result as Entry[] : null;
  } catch { return null; }
}

export function SpecialPracticeQuestionEditor({ kind, value, onChange, vi }: Props) {
  const entries = parse(value);
  const update = (next: Entry[]) => onChange(JSON.stringify(next, null, 2));
  const setEntry = (index: number, patch: Entry) => {
    if (!entries) return;
    update(entries.map((entry, i) => i === index ? { ...entry, ...patch } : entry));
  };
  const removeEntry = (index: number) => { if (entries) update(entries.filter((_, i) => i !== index)); };
  const setQuestion = (sectionIndex: number, questionIndex: number, patch: Entry) => {
    if (!entries) return;
    const section = entries[sectionIndex];
    const questions = Array.isArray(section.questions) ? section.questions as Entry[] : [];
    setEntry(sectionIndex, { questions: questions.map((question, i) => i === questionIndex ? { ...question, ...patch } : question) });
  };
  const field = (label: string, value: unknown, set: (text: string) => void, multiline = false) =>
    <label className="block text-xs font-semibold">{label}{multiline
      ? <textarea value={String(value ?? '')} onChange={event => set(event.target.value)} rows={3} className="mt-1 w-full rounded-lg border-notebook p-2 font-normal" />
      : <input value={String(value ?? '')} onChange={event => set(event.target.value)} className="mt-1 w-full rounded-lg border-notebook p-2 font-normal" />}</label>;

  return <div className="space-y-3">
    <h3 className="text-sm font-bold">{kind === 'certificate' ? (vi ? 'Các phần thi' : 'Exam sections') : (vi ? 'Các lượt phỏng vấn' : 'Interview turns')}</h3>
    {entries === null && <p role="alert" className="text-xs text-rose-700">{vi ? 'JSON câu hỏi chưa hợp lệ. Sửa trong ô nâng cao bên dưới.' : 'Question JSON is invalid. Fix it in the advanced editor below.'}</p>}
    {entries?.map((entry, sectionIndex) => kind === 'interview'
      ? <div key={String(entry.id ?? sectionIndex)} className="space-y-2 rounded-lg border-notebook bg-white p-3">
          <div className="flex justify-between"><strong className="text-xs">{vi ? 'Lượt' : 'Turn'} {sectionIndex + 1}</strong><button type="button" onClick={() => removeEntry(sectionIndex)} className="text-xs text-red-700">{vi ? 'Xóa' : 'Delete'}</button></div>
          {field(vi ? 'Câu hỏi' : 'Prompt', entry.prompt, text => setEntry(sectionIndex, { prompt: text }), true)}
          {field(vi ? 'Bối cảnh (tùy chọn)' : 'Context (optional)', entry.context, text => setEntry(sectionIndex, { context: text }))}
          <label className="block text-xs font-semibold">{vi ? 'Điểm tối đa' : 'Maximum points'}<input type="number" min={1} max={100} value={Number(entry.points ?? 10)} onChange={event => setEntry(sectionIndex, { points: Number(event.target.value) })} className="mt-1 w-28 rounded-lg border-notebook p-2" /></label>
        </div>
      : <div key={sectionIndex} className="space-y-3 rounded-lg border-notebook bg-white p-3">
          <div className="flex flex-wrap items-center justify-between gap-2"><label className="text-xs font-semibold">{vi ? 'Kỹ năng' : 'Skill'} <select value={String(entry.skill ?? 'listening')} onChange={event => {
            const skill = event.target.value;
            const questions = (Array.isArray(entry.questions) ? entry.questions as Entry[] : []).map(question => {
              const next = { ...question };
              if (skill === 'speaking' || skill === 'writing') { delete next.options; delete next.correctIndex; next.rubric ??= ''; }
              else { next.options ??= ['', '']; next.correctIndex ??= 0; }
              return next;
            });
            setEntry(sectionIndex, { skill, questions });
          }} className="rounded-lg border-notebook p-2"><option value="listening">Listening</option><option value="reading">Reading</option><option value="speaking">Speaking</option><option value="writing">Writing</option></select></label><button type="button" onClick={() => removeEntry(sectionIndex)} className="text-xs text-red-700">{vi ? 'Xóa phần thi' : 'Delete section'}</button></div>
          {(Array.isArray(entry.questions) ? entry.questions as Entry[] : []).map((question, questionIndex) => {
            const objective = entry.skill === 'listening' || entry.skill === 'reading';
            return <div key={String(question.id ?? questionIndex)} className="space-y-2 rounded-lg bg-stone-50 p-3">
              <div className="flex justify-between"><strong className="text-xs">{vi ? 'Câu' : 'Question'} {questionIndex + 1}</strong><button type="button" onClick={() => setEntry(sectionIndex, { questions: (entry.questions as Entry[]).filter((_, i) => i !== questionIndex) })} className="text-xs text-red-700">{vi ? 'Xóa câu' : 'Delete question'}</button></div>
              {field(vi ? 'Đề bài' : 'Prompt', question.prompt, text => setQuestion(sectionIndex, questionIndex, { prompt: text }), true)}
              {entry.skill === 'reading' && field(vi ? 'Đoạn đọc' : 'Passage', question.passage, text => setQuestion(sectionIndex, questionIndex, { passage: text }), true)}
              {entry.skill === 'listening' && <>{field(vi ? 'Bản thoại để phát' : 'Audio script', question.audioScript, text => setQuestion(sectionIndex, questionIndex, { audioScript: text }), true)}{field(vi ? 'URL âm thanh (tùy chọn)' : 'Audio URL (optional)', question.audioUrl, text => setQuestion(sectionIndex, questionIndex, { audioUrl: text }))}</>}
              {objective ? <>{field(vi ? 'Các lựa chọn, mỗi dòng một đáp án' : 'Options, one per line', Array.isArray(question.options) ? question.options.join('\n') : '', text => setQuestion(sectionIndex, questionIndex, { options: text.split('\n').map(item => item.trim()) }), true)}
                <label className="block text-xs font-semibold">{vi ? 'Đáp án đúng' : 'Correct answer'} <select value={Number(question.correctIndex ?? 0)} onChange={event => setQuestion(sectionIndex, questionIndex, { correctIndex: Number(event.target.value) })} className="rounded-lg border-notebook p-2">{(Array.isArray(question.options) ? question.options : []).map((option, i) => <option key={i} value={i}>{i + 1}. {String(option)}</option>)}</select></label></>
                : field('Rubric', question.rubric, text => setQuestion(sectionIndex, questionIndex, { rubric: text }), true)}
              <label className="block text-xs font-semibold">{vi ? 'Điểm tối đa' : 'Maximum points'}<input type="number" min={1} max={100} value={Number(question.points ?? 1)} onChange={event => setQuestion(sectionIndex, questionIndex, { points: Number(event.target.value) })} className="mt-1 w-28 rounded-lg border-notebook p-2" /></label>
            </div>;
          })}
          <button type="button" onClick={() => setEntry(sectionIndex, { questions: [...(Array.isArray(entry.questions) ? entry.questions : []), { id: crypto.randomUUID(), prompt: '', points: 1, ...(entry.skill === 'listening' || entry.skill === 'reading' ? { options: ['', ''], correctIndex: 0 } : { rubric: '' }) }] })} className="rounded-lg border-notebook px-3 py-2 text-xs">+ {vi ? 'Thêm câu hỏi' : 'Add question'}</button>
        </div>)}
    <button type="button" onClick={() => update([...(entries ?? []), kind === 'interview'
      ? { id: crypto.randomUUID(), prompt: '', points: 10 }
      : { skill: 'listening', questions: [{ id: crypto.randomUUID(), prompt: '', audioScript: '', options: ['', ''], correctIndex: 0, points: 1 }] }])} className="rounded-lg border-notebook px-3 py-2 text-xs">+ {kind === 'certificate' ? (vi ? 'Thêm phần thi' : 'Add section') : (vi ? 'Thêm lượt hỏi' : 'Add turn')}</button>
    <details><summary className="cursor-pointer text-xs text-stone-600">{vi ? 'Sửa JSON nâng cao' : 'Advanced JSON editor'}</summary><textarea value={value} onChange={event => onChange(event.target.value)} rows={12} spellCheck={false} className="mt-2 w-full rounded-lg border-notebook p-2 font-mono text-xs" /></details>
  </div>;
}
