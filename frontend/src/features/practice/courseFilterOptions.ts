import type { SelectOption } from '../../components/ui/NotebookSelect';
import { curriculumStore } from '../../services/curriculumStore';
import { getDomainTitle } from '../../utils/languageLevels';
import type { StudyTarget } from '../../types';

export function getCourseFilterOptions(
  allLanguagesLabel: string,
  allDomainsLabel: string,
  locale: 'en' | 'vi',
  target?: StudyTarget | null
): { languages: SelectOption[]; domains: SelectOption[] } {
  const courses = curriculumStore.getAllCourses();
  const languageCodes = target?.languageCode ? [target.languageCode] : [...new Set(courses.map((course) => course.languageCode).filter(Boolean))].sort();
  const domainCodes = target ? (target.domains?.length ? target.domains : [target.domain]) : [...new Set(courses.map((course) => course.domain).filter(Boolean))].sort();

  return {
    languages: [
      { value: 'all', label: allLanguagesLabel },
      ...languageCodes.map((code) => ({ value: code, label: code.toUpperCase() }))
    ],
    domains: [
      { value: 'all', label: allDomainsLabel },
      ...domainCodes.map((code) => ({ value: code, label: getDomainTitle(code, locale) }))
    ]
  };
}
