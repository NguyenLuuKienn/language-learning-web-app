import React, { useState, useEffect, useMemo } from 'react';
import { useTranslation } from '../../i18n';
import type { StudyTarget } from '../../types';
import { curriculumStore, type GrammarItem } from '../../services/curriculumStore';
import { getDomainTitle } from '../../utils/languageLevels';
import { NotebookSelect } from '../../components/ui/NotebookSelect';
import { getCourseFilterOptions } from './courseFilterOptions';
import { playPracticeFeedback } from '../../services/practiceFeedback';
import { SkillProgressBar } from './SkillProgressBar';
import { useSkillProgress } from './useSkillProgress';
import { App } from 'antd';
import { getErrorMessage } from '../../services/apiClient';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  ArrowLeftOutlined,
  BulbOutlined,
  CloseOutlined,
  BookOutlined,
  SearchOutlined,
  LeftOutlined,
  RightOutlined
} from '@ant-design/icons';

interface GrammarViewProps {
  target: StudyTarget | null;
  onBack: () => void;
}

export const GrammarView: React.FC<GrammarViewProps> = ({ target, onBack }) => {
  const { t, language } = useTranslation();
  const { message } = App.useApp();

  // Filters
  const [filterLang, setFilterLang] = useState<string>(target?.languageCode || 'all');
  const [filterDomain, setFilterDomain] = useState<string>(target?.domain || 'all');
  const filterLevel = target?.level || 'all';

  const [topics, setTopics] = useState<(GrammarItem & { courseId: string; courseTitle: string; level: string; domain: string; languageCode: string })[]>([]);
  const progress = useSkillProgress('grammar', topics);
  const [selectedTopicId, setSelectedTopicId] = useState<string>('');

  // Quiz state
  const [selectedQuizOption, setSelectedQuizOption] = useState<number | null>(null);
  const [isAnswerSubmitted, setIsAnswerSubmitted] = useState<boolean>(false);
  const [isCorrect, setIsCorrect] = useState(false);

  const resetQuiz = () => {
    setSelectedQuizOption(null);
    setIsAnswerSubmitted(false);
  };

  const loadTopics = () => {
    const list = curriculumStore.getGrammarForTarget(target, filterLang, filterDomain, filterLevel);
    setTopics(list);
    setSelectedTopicId('');
    resetQuiz();
  };

  useEffect(() => {
    loadTopics();
  }, [target, filterLang, filterDomain, filterLevel]);
  useEffect(() => { if (progress.loaded && !selectedTopicId && topics.length) setSelectedTopicId(progress.resumeId || topics[0].id); }, [progress.loaded, progress.resumeId, selectedTopicId, topics]);
  useEffect(() => { const selected = topics.find((item) => item.id === selectedTopicId); if (progress.loaded && selected) void progress.record(selected).catch(() => undefined); }, [progress.loaded, selectedTopicId]);

  const checkAnswer = async () => {
    if (!selectedTopic || selectedQuizOption === null) return;
    try {
      const result = await progress.record(selectedTopic, selectedQuizOption);
      setIsCorrect(result.correct === true);
      setIsAnswerSubmitted(true);
      playPracticeFeedback(result.correct === true);
    } catch (error) { message.error(getErrorMessage(error, t('common.error'))); }
  };

  const [searchQuery, setSearchQuery] = useState('');

  const searchedTopics = useMemo(() => {
    if (!searchQuery.trim()) return topics;
    const q = searchQuery.toLowerCase().trim();
    return topics.filter(
      (t) =>
        t.title.toLowerCase().includes(q) ||
        t.formula.toLowerCase().includes(q) ||
        t.explanation?.toLowerCase().includes(q)
    );
  }, [topics, searchQuery]);

  const selectedTopic = searchedTopics.find((t) => t.id === selectedTopicId) || searchedTopics[0] || topics[0];

  const currentIndex = searchedTopics.findIndex((t) => t.id === selectedTopic?.id);
  const prevTopic = currentIndex > 0 ? searchedTopics[currentIndex - 1] : null;
  const nextTopic = currentIndex >= 0 && currentIndex < searchedTopics.length - 1 ? searchedTopics[currentIndex + 1] : null;

  // Dropdown options for NotebookSelect
  const { languages: langOptions, domains: domainOptions } = getCourseFilterOptions(
    t('common.allLanguages'),
    t('common.allDomains'),
    language
  );



  return (
    <div className="space-y-6 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b-2 border-stone-300">
        <button
          onClick={onBack}
          className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold text-stone-700 hover:bg-stone-200 transition notebook-btn bg-white cursor-pointer self-start"
        >
          <ArrowLeftOutlined />
          <span>{t('common.back')}</span>
        </button>


      </div>

      {target && <SkillProgressBar completed={progress.completed} total={topics.length} language={language} error={progress.error} hasResume={!!progress.resumeId} onResume={() => { setSelectedTopicId(progress.resumeId); resetQuiz(); }} />}

      {/* Filter Bar */}
      <div className="bg-white border-notebook rounded-2xl p-4 shadow-xs flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2.5">
          <span className="text-xs font-bold text-stone-700 uppercase tracking-wider">
            {t('common.filter')}:
          </span>

          <NotebookSelect
            value={filterLang}
            onChange={(val) => setFilterLang(val)}
            options={langOptions.filter((option) => option.value === target?.languageCode)}
            className="w-44"
          />

          <NotebookSelect
            value={filterDomain}
            onChange={(val) => setFilterDomain(val)}
            options={domainOptions.filter((option) => (target?.domains?.length ? target.domains : [target?.domain]).includes(option.value) || (option.value === 'all' && (target?.domains?.length || 0) > 1))}
            className="w-48"
          />

          <div className="px-3 py-1.5 rounded-xl bg-amber-50 border border-amber-300 text-xs font-bold text-amber-900 flex items-center gap-1.5" title="Trình độ học tập hiện tại của bạn">
            <span className="text-[10px] text-amber-700 font-mono">{t('courses.level')}:</span>
            <span>{target?.level || 'A1'}</span>
          </div>
        </div>

        <span className="text-xs text-stone-500 font-mono">
          {topics.length} {t('grammar.topicCount')}
        </span>
      </div>

      {/* Topics Selector & Detail (2-Column Course-like Layout) */}
      {topics.length > 0 ? (
        <div className="flex flex-col md:flex-row items-start gap-5">
          {/* CỘT TRÁI: Sidebar danh mục chủ điểm */}
          <div className="w-full md:w-80 shrink-0 bg-white border-notebook rounded-2xl p-4 shadow-xs space-y-3">
            <div className="flex items-center justify-between border-b border-stone-200 pb-2.5">
              <span className="text-xs font-bold text-stone-900 uppercase tracking-wider font-mono">
                {t('grammar.topicList')}
              </span>
              <span className="text-[11px] font-mono font-bold text-stone-500 bg-stone-100 px-2 py-0.5 rounded">
                {searchedTopics.length} / {topics.length}
              </span>
            </div>

            {/* Ô tìm kiếm nhanh */}
            <div className="relative">
              <SearchOutlined className="absolute left-2.5 top-1/2 -translate-y-1/2 text-stone-400 text-xs" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={t('grammar.searchTopics')}
                className="w-full pl-8 pr-3 py-1.5 bg-stone-50 border border-stone-200 rounded-lg text-xs outline-none focus:border-stone-800 transition"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-stone-400 hover:text-stone-700 text-xs"
                >
                  <CloseOutlined />
                </button>
              )}
            </div>

            {/* Danh sách cuộn chủ điểm */}
            <div className="max-h-[620px] overflow-y-auto space-y-1.5 pr-1 text-xs">
              {searchedTopics.length === 0 ? (
                <div className="p-6 text-center text-stone-400 text-xs italic">
                  {t('grammar.noSearchResults')}
                </div>
              ) : (
                searchedTopics.map((item, idx) => {
                  const isSelected = item.id === (selectedTopic?.id || '');
                  return (
                    <button
                      key={item.id}
                      type="button"
                      onClick={() => {
                        setSelectedTopicId(item.id);
                        resetQuiz();
                      }}
                      className={`w-full text-left p-2.5 rounded-xl border transition cursor-pointer flex flex-col gap-1 ${
                        isSelected
                          ? 'bg-stone-900 text-white border-stone-900 shadow-2xs'
                          : 'bg-stone-50/60 hover:bg-stone-100/90 border-stone-200 text-stone-800'
                      }`}
                    >
                      <div className="flex items-center justify-between gap-1.5">
                        <span
                          className={`text-[10px] font-mono font-bold px-1.5 py-0.2 rounded shrink-0 ${
                            isSelected ? 'bg-white/20 text-white' : 'bg-stone-200 text-stone-700'
                          }`}
                        >
                          {item.level || 'A1'} #{idx + 1}
                        </span>
                        <span className={`text-[10px] truncate font-mono ${isSelected ? 'text-stone-300' : 'text-stone-400'}`}>
                          {item.domain}
                        </span>
                      </div>
                      <span className="font-bold text-xs line-clamp-2 leading-snug">
                        {item.title}
                      </span>
                      {item.formula && (
                        <span
                          className={`text-[11px] font-mono truncate ${
                            isSelected ? 'text-amber-300' : 'text-stone-500'
                          }`}
                        >
                          {item.formula}
                        </span>
                      )}
                    </button>
                  );
                })
              )}
            </div>
          </div>

          {/* CỘT PHẢI: Nội dung chi tiết chủ điểm */}
          <div className="flex-1 min-w-0 w-full">
            {selectedTopic ? (
              <div className="bg-white border-notebook-2 rounded-2xl p-6 shadow-notebook space-y-6">
                {/* Header bài học */}
                <div className="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-stone-200">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-indigo-50 text-indigo-700 border border-indigo-200 uppercase">
                        {selectedTopic.level} • {getDomainTitle(selectedTopic.domain, language)}
                      </span>
                      <span className="text-xs text-stone-400 font-mono">
                        {selectedTopic.languageCode.toUpperCase()}
                      </span>
                    </div>
                    <h2 className="text-lg font-bold text-stone-900 mt-1">{selectedTopic.title}</h2>
                    <div className="flex items-center gap-1 text-xs text-stone-500 mt-0.5">
                      <BookOutlined className="text-stone-400" />
                      <span>{t('listening.fromCourse')}: <strong>{selectedTopic.courseTitle}</strong></span>
                    </div>
                  </div>
                </div>

                {/* Formula & Rule Box */}
                <div className="p-4 bg-amber-50/70 border-notebook rounded-xl space-y-2">
                  <span className="text-xs font-bold text-amber-900 uppercase tracking-wider font-mono flex items-center gap-1.5">
                    <BulbOutlined />
                    <span>{t('grammar.formula')}</span>
                  </span>
                  {selectedTopic.formula && <div className="p-3 bg-white border border-stone-300 rounded-lg text-sm font-mono font-bold text-stone-900">{selectedTopic.formula}</div>}
                  {selectedTopic.explanation && (
                    <p className="text-xs text-stone-700 leading-relaxed pt-1">
                      {selectedTopic.explanation}
                    </p>
                  )}
                </div>

                {/* Sample Sentences */}
                {(selectedTopic.example || selectedTopic.examples?.length) && <div className="space-y-2">
                  <span className="text-xs font-bold text-stone-500 uppercase tracking-wider block font-mono">
                    {t('grammar.example')}
                  </span>
                  <div className="p-3.5 bg-stone-50 border border-stone-200 rounded-xl space-y-1">
                    {(selectedTopic.examples?.length ? selectedTopic.examples : [selectedTopic.example]).map((example, index) => <p key={`${selectedTopic.id}-${index}`} className="text-sm font-mono font-bold text-stone-900">{example}</p>)}
                    {selectedTopic.exampleTranslation && <p className="text-xs text-stone-600 italic">{selectedTopic.exampleTranslation}</p>}
                  </div>
                </div>}

                {selectedTopic.question && (!selectedTopic.options || selectedTopic.options.length < 2) && (
                  <div className="rounded-xl border border-stone-200 bg-stone-50 p-4 text-xs">
                    <strong className="block mb-2">{t('grammar.practicePrompt')}</strong>
                    <p>{selectedTopic.question}</p>
                  </div>
                )}

                {/* Practice Quiz */}
                {selectedTopic.question && selectedTopic.options?.length >= 2 && (
                  <div className="pt-4 border-t border-stone-200 space-y-4">
                    <h3 className="text-sm font-bold text-stone-900 flex items-center gap-2">
                      <span className="w-5 h-5 rounded-full bg-stone-900 text-white flex items-center justify-center text-[10px] font-mono">
                        ?
                      </span>
                      <span>{t('grammar.practiceQuiz')}: {selectedTopic.question}</span>
                    </h3>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                      {selectedTopic.options.map((opt, idx) => {
                        const isSelected = selectedQuizOption === idx;
                        const isCorrect = idx === selectedTopic.correctIndex;
                        let optStyle = 'bg-stone-50 border-stone-200 hover:border-stone-400 text-stone-800';

                        if (isAnswerSubmitted) {
                          if (isCorrect) {
                            optStyle = 'bg-emerald-50 border-emerald-500 text-emerald-900 font-bold';
                          } else if (isSelected) {
                            optStyle = 'bg-rose-50 border-rose-500 text-rose-900 font-bold';
                          }
                        } else if (isSelected) {
                          optStyle = 'bg-amber-100 border-stone-800 text-stone-900 font-bold ring-2 ring-amber-300';
                        }

                        return (
                          <button
                            key={idx}
                            type="button"
                            disabled={isAnswerSubmitted}
                            onClick={() => setSelectedQuizOption(idx)}
                            className={`w-full text-left p-3 rounded-xl border text-xs transition flex items-center justify-between cursor-pointer ${optStyle}`}
                          >
                            <div className="flex items-center gap-2">
                              <span className="w-5 h-5 rounded bg-white border border-stone-300 flex items-center justify-center text-[11px] font-mono font-bold text-stone-600">
                                {String.fromCharCode(65 + idx)}
                              </span>
                              <span>{opt}</span>
                            </div>

                            {isAnswerSubmitted && isCorrect && (
                              <CheckCircleOutlined className="text-emerald-600 text-sm" />
                            )}
                            {isAnswerSubmitted && isSelected && !isCorrect && (
                              <CloseCircleOutlined className="text-rose-600 text-sm" />
                            )}
                          </button>
                        );
                      })}
                    </div>

                    {!isAnswerSubmitted ? (
                      <button
                        disabled={selectedQuizOption === null}
                        onClick={() => void checkAnswer()}
                        className="px-6 py-2 bg-stone-900 text-white text-xs font-bold rounded-xl notebook-btn hover:bg-stone-800 transition disabled:opacity-40 cursor-pointer"
                      >
                        {t('listening.checkAnswer')}
                      </button>
                    ) : (
                      <div className="p-3 bg-stone-50 border border-stone-200 rounded-xl flex items-center justify-between">
                        <span className={`text-xs font-bold ${selectedQuizOption === selectedTopic.correctIndex ? 'text-emerald-700' : 'text-rose-700'}`}>
                          {isCorrect ? t('listening.correct') : t('listening.incorrect')}
                        </span>
                        <button
                          type="button"
                          onClick={resetQuiz}
                          className="text-xs font-bold text-stone-700 hover:underline cursor-pointer"
                        >
                          {t('grammar.retryQuestion')}
                        </button>
                      </div>
                    )}
                  </div>
                )}

                {/* Footer Điều hướng Prev / Next */}
                <div className="pt-4 border-t border-stone-200 flex items-center justify-between gap-3">
                  <button
                    type="button"
                    disabled={!prevTopic}
                    onClick={() => {
                      if (prevTopic) {
                        setSelectedTopicId(prevTopic.id);
                        resetQuiz();
                      }
                    }}
                    className="px-3.5 py-1.5 rounded-lg border border-stone-300 text-xs font-bold text-stone-700 hover:bg-stone-100 disabled:opacity-30 disabled:cursor-not-allowed transition flex items-center gap-1.5 cursor-pointer"
                  >
                    <LeftOutlined className="text-[10px]" />
                    <span>{t('grammar.previousTopic')}</span>
                  </button>

                  <span className="text-[11px] font-mono text-stone-400">
                    {currentIndex + 1} / {searchedTopics.length}
                  </span>

                  <button
                    type="button"
                    disabled={!nextTopic}
                    onClick={() => {
                      if (nextTopic) {
                        setSelectedTopicId(nextTopic.id);
                        resetQuiz();
                      }
                    }}
                    className="px-3.5 py-1.5 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn hover:bg-stone-800 disabled:opacity-30 disabled:cursor-not-allowed transition flex items-center gap-1.5 cursor-pointer"
                  >
                    <span>{t('grammar.nextTopic')}</span>
                    <RightOutlined className="text-[10px]" />
                  </button>
                </div>
              </div>
            ) : (
              <div className="bg-white border-notebook-2 rounded-2xl p-12 text-center text-stone-400 text-xs">
                {t('grammar.selectTopic')}
              </div>
            )}
          </div>
        </div>
      ) : (
        <div className="bg-white border-notebook-2 rounded-2xl p-12 text-center space-y-3">
          <BookOutlined className="text-3xl text-stone-300 mx-auto block" />
          <p className="text-xs font-bold text-stone-600">
            {t('grammar.noTopics')}
          </p>
          <p className="text-[11px] text-stone-400">
            {t('grammar.noTopicsHint')}
          </p>
        </div>
      )}

    </div>
  );
};
