import React from 'react';
import { CompassOutlined } from '@ant-design/icons';
import { useTranslation } from '../../i18n';

interface Props { onChooseGoal: () => void }

export const StudyGoalRequired: React.FC<Props> = ({ onChooseGoal }) => {
  const { language } = useTranslation();
  const vi = language === 'vi';
  return <section className="mx-auto max-w-3xl rounded-2xl border-notebook-2 bg-white p-6" role="status">
    <CompassOutlined className="text-2xl text-amber-700" />
    <h1 className="mt-3 text-xl font-bold">{vi ? 'Bắt đầu hành trình học' : 'Start your learning journey'}</h1>
    <p className="mt-2 text-sm text-stone-600">{vi
      ? 'Chọn mục tiêu học và hoàn thành bài đánh giá đầu vào. Sau đó, bạn sẽ thấy khóa học, bài tiếp theo và các công cụ luyện tập phù hợp.'
      : 'Choose a study goal and complete the placement assessment. Your courses, next lesson and matching practice tools will then appear here.'}</p>
    <button type="button" onClick={onChooseGoal} className="mt-5 rounded-lg bg-amber-300 px-5 py-3 text-sm font-bold hover:bg-amber-400">
      {vi ? 'Chọn mục tiêu và bắt đầu' : 'Choose a goal and start'}
    </button>
  </section>;
};
