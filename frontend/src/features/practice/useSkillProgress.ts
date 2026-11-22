import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { skillPracticeApi, type SkillKind, type SkillPracticeState } from '../../services/apiClient';
import { getErrorMessage } from '../../services/apiClient';

interface Activity { id: string; courseId: string }

function nextActivityId(activities: Activity[], states: SkillPracticeState[]): string {
  if (!states.length) return '';
  const started = states.find((item) => item.status === 'InProgress' &&
    activities.some((activity) => activity.courseId === item.courseId && activity.id === item.activityId));
  if (started) return started.activityId;
  const unfinished = activities.find((activity) => !states.some((item) =>
    item.courseId === activity.courseId && item.activityId === activity.id &&
    (item.status === 'Completed' || item.status === 'Practiced')));
  return unfinished?.id ?? states[0].activityId;
}

export function useSkillProgress(kind: SkillKind, activities: Activity[]) {
  const [states, setStates] = useState<SkillPracticeState[]>([]);
  const [resumeId, setResumeId] = useState('');
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState('');
  const pendingAttempts = useRef(new Map<string, string>());
  const ids = useMemo(() => new Set(activities.map((item) => `${item.courseId}:${item.id}`)), [activities]);

  useEffect(() => {
    let active = true;
    setLoaded(false);
    setError('');
    skillPracticeApi.list(kind).then((items) => {
      if (!active) return;
      const scoped = items.filter((item) => ids.has(`${item.courseId}:${item.activityId}`));
      setStates(scoped);
      setResumeId(nextActivityId(activities, scoped));
      setLoaded(true);
    }).catch((cause) => { if (active) { setError(getErrorMessage(cause, 'Could not load progress.')); setStates([]); setLoaded(true); } });
    return () => { active = false; };
  }, [kind, ids, activities]);

  const record = useCallback(async (activity: Activity, answerIndex?: number, practiced = false) => {
    const key = `${kind}:${activity.courseId}:${activity.id}:${answerIndex ?? ''}:${practiced}`;
    const clientAttemptId = pendingAttempts.current.get(key) ?? crypto.randomUUID();
    pendingAttempts.current.set(key, clientAttemptId);
    try {
      const result = await skillPracticeApi.record(kind, activity.courseId, activity.id, answerIndex, practiced, clientAttemptId);
      pendingAttempts.current.delete(key);
      const updated = [result, ...states.filter((item) => item.courseId !== result.courseId || item.activityId !== result.activityId)];
      setStates(updated);
      setResumeId(nextActivityId(activities, updated));
      setError('');
      return result;
    } catch (cause) { setError(getErrorMessage(cause, 'Could not save progress.')); throw cause; }
  }, [kind, activities, states]);

  return {
    states, resumeId, loaded, error, record,
    completed: states.filter((item) => item.status === 'Completed').length,
    practiced: states.filter((item) => item.status === 'Practiced').length
  };
}
