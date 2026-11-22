import React, { useState, useEffect, useRef } from 'react';
import { App, Dropdown, Modal } from 'antd';
import { useTranslation } from '../../i18n';
import { adminApi, courseCategoriesApi, coursesApi, getErrorMessage } from '../../services/apiClient';
import type {
  AdminUser,
  AuditLog,
  UserRole,
  Language,
  Course,
  CourseDetail,
  StudyTarget,
  CurriculumExportResponse,
  CurriculumImportResultResponse,
  CreateLanguagePayload
} from '../../types';
import {
  getLevelsForLanguage,
  getDefaultLevelForLanguage,
  DOMAINS,
  getDomainTitle,
  applyCourseCategories
} from '../../utils/languageLevels';
import { CourseCategoryManager } from './CourseCategoryManager';
import { ContentQualityPanel } from './ContentQualityPanel';
import { SrsEvaluationPanel } from './SrsEvaluationPanel';
import { curriculumStore } from '../../services/curriculumStore';
import { createCourseMaterialTemplate, parseCourseMaterial } from '../../services/courseMaterial';
import { PlacementEditor } from './PlacementEditor';
import { SpecialPracticeEditor, type SpecialPracticeSection } from './SpecialPracticeEditor';
import { SpecialPracticeReviewPanel } from './SpecialPracticeReviewPanel';
import { LessonMaterialEditor } from './LessonMaterialEditor';
import { PlacementReviewPanel } from './PlacementReviewPanel';
import { examStore } from '../../services/examStore';
import { NotebookSelect } from '../../components/ui/NotebookSelect';
import { Pagination } from '../../components/ui/Pagination';
import { sortUnits, sortLessons, sortCourses, sortLanguages } from '../../utils/sorting';
import {
  SafetyCertificateOutlined,
  AuditOutlined,
  UserOutlined,
  GlobalOutlined,
  SearchOutlined,
  LockOutlined,
  UnlockOutlined,
  LoadingOutlined,
  PlusOutlined,
  DownloadOutlined,
  UploadOutlined,
  DeleteOutlined,
  BookOutlined,
  CheckCircleOutlined,
  CloseOutlined,
  FileTextOutlined,
  AppstoreOutlined,
  ThunderboltOutlined,
  FolderOpenOutlined,
  FolderOutlined,
  MoreOutlined,
} from '@ant-design/icons';

const COURSE_PAGE_SIZE = 8;

export const AdminView: React.FC<{ target?: StudyTarget | null }> = ({ target }) => {
  const { message } = App.useApp();
  const { t, language } = useTranslation();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [coursePage, setCoursePage] = useState(1);
  const [activeTab, setActiveTab] = useState<'dashboard' | 'curriculum' | 'quality' | 'users' | 'audit'>('dashboard');
  const [searchTerm, setSearchTerm] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const usersRequestId = useRef(0);
  const curriculumRequestId = useRef(0);

  const [selectedLanguageFilter, setSelectedLanguageFilter] = useState<string>(target?.languageId || '');
  const [selectedLevelFilter, setSelectedLevelFilter] = useState<string>('');
  const [selectedDomainFilter, setSelectedDomainFilter] = useState<string>('');

  const [isLanguageModalOpen, setIsLanguageModalOpen] = useState(false);
  const [isCategoryManagerOpen, setIsCategoryManagerOpen] = useState(false);
  const [categoryRevision, setCategoryRevision] = useState(0);
  const [newLangCode, setNewLangCode] = useState('');
  const [newLangName, setNewLangName] = useState('');
  const [newLangNative, setNewLangNative] = useState('');
  const [newLangFlag, setNewLangFlag] = useState('');

  // Course Creation State
  const [isCourseModalOpen, setIsCourseModalOpen] = useState(false);
  const [editingCourse, setEditingCourse] = useState<Course | null>(null);
  const [courseLangId, setCourseLangId] = useState('');
  const [courseTitle, setCourseTitle] = useState('');
  const [courseTitleEdited, setCourseTitleEdited] = useState(false);
  const [courseLevel, setCourseLevel] = useState('A1');
  const [courseDomain, setCourseDomain] = useState('general');
  const [courseDesc, setCourseDesc] = useState('');
  const [isCreatingCourse, setIsCreatingCourse] = useState(false);

  // Manage Course Units & Lessons State
  const [managedCourse, setManagedCourse] = useState<CourseDetail | null>(null);
  const [isManageModalOpen, setIsManageModalOpen] = useState(false);
  const [isPlacementEditorOpen, setIsPlacementEditorOpen] = useState(false);
  const [isSpecialPracticeEditorOpen, setIsSpecialPracticeEditorOpen] = useState(false);
  const [specialPracticeSection, setSpecialPracticeSection] = useState<SpecialPracticeSection>('IELTS');
  const [isSpecialPracticeReviewOpen, setIsSpecialPracticeReviewOpen] = useState(false);
  const [editingLessonId, setEditingLessonId] = useState<string | null>(null);
  const [isPlacementReviewOpen, setIsPlacementReviewOpen] = useState(false);
  const [isLoadingCourseDetail, setIsLoadingCourseDetail] = useState(false);
  const [newUnitTitle, setNewUnitTitle] = useState('');
  const [activeUnitForLesson, setActiveUnitForLesson] = useState<string | null>(null);
  const [newLessonTitle, setNewLessonTitle] = useState('');
  const [newLessonType, setNewLessonType] = useState('Vocabulary');

  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [importJsonText, setImportJsonText] = useState('');
  const [importReplace, setImportReplace] = useState(false);
  const [importResult, setImportResult] = useState<CurriculumImportResultResponse | null>(null);
  const [isImporting, setIsImporting] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);

  // Quản lý Import JSON riêng cho từng khóa học
  const [isCourseImportModalOpen, setIsCourseImportModalOpen] = useState(false);
  const [courseImportJsonText, setCourseImportJsonText] = useState('');
  const [courseImportError, setCourseImportError] = useState('');
  const singleCourseFileInputRef = useRef<HTMLInputElement>(null);

  const loadUsers = async (search?: string) => {
    const requestId = ++usersRequestId.current;
    setIsLoading(true);
    try {
      const data = await adminApi.getUsers(search);
      if (requestId === usersRequestId.current) setUsers(data);
    } catch (error) {
      if (requestId === usersRequestId.current) {
        setUsers([]);
        message.error(getErrorMessage(error, t('common.error')));
      }
    } finally {
      if (requestId === usersRequestId.current) setIsLoading(false);
    }
  };

  const loadAuditLogs = async () => {
    try {
      const logs = await adminApi.getAuditLogs();
      setAuditLogs(logs);
    } catch (error) {
      setAuditLogs([]);
      message.error(getErrorMessage(error, t('common.error')));
    }
  };

  const loadCurriculumData = async () => {
    const requestId = ++curriculumRequestId.current;
    try {
      const [langs, crs, categories] = await Promise.all([
        coursesApi.getLanguages(),
        coursesApi.getCourses(),
        courseCategoriesApi.list().catch(() => null)
      ]);
      if (requestId === curriculumRequestId.current) {
        if (categories) {
          applyCourseCategories(categories);
          setCategoryRevision(value => value + 1);
        }
        setLanguages(sortLanguages(langs));
        setCourses(sortCourses(crs));
      }
    } catch (error) {
      if (requestId === curriculumRequestId.current) {
        setLanguages([]);
        setCourses([]);
        message.error(getErrorMessage(error, t('common.error')));
      }
    }
  };

  useEffect(() => {
    if (activeTab === 'dashboard' || activeTab === 'curriculum' || activeTab === 'quality') void loadCurriculumData();
  }, [activeTab]);

  useEffect(() => {
    if (activeTab === 'dashboard' || activeTab === 'users') void loadUsers();
  }, [activeTab]);

  useEffect(() => {
    if (activeTab === 'dashboard' || activeTab === 'audit') void loadAuditLogs();
  }, [activeTab]);

  const handleToggleStatus = async (user: AdminUser) => {
    try {
      const updated = await adminApi.updateUserStatus(user.id, !user.isActive);
      setUsers((prev) => prev.map((u) => (u.id === user.id ? updated : u)));
      message.success(t('common.success'));
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleToggleRole = async (user: AdminUser) => {
    const nextRole: UserRole = user.role === 'Admin' ? 'Learner' : 'Admin';
    try {
      const updated = await adminApi.updateUserRole(user.id, nextRole);
      setUsers((prev) => prev.map((u) => (u.id === user.id ? updated : u)));
      message.success(t('common.success'));
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleCreateLanguage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newLangCode || !newLangName) return;
    try {
      const payload: CreateLanguagePayload = {
        code: newLangCode.trim().toLowerCase(),
        name: newLangName.trim(),
        nativeName: newLangNative.trim() || undefined,
        flagEmoji: newLangFlag.trim() || '🌐'
      };
      const created = await adminApi.createLanguage(payload);
      setLanguages((prev) => sortLanguages([...prev, created]));
      setIsLanguageModalOpen(false);
      setNewLangCode('');
      setNewLangName('');
      setNewLangNative('');
      setNewLangFlag('');
      message.success(t('common.success'));
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleDeleteLanguage = (id: string) => {
    Modal.confirm({
      title: t('admin.confirmDeleteLanguage'),
      okText: t('common.delete'),
      okType: 'danger',
      cancelText: t('common.cancel'),
      onOk: async () => {
        try {
          await adminApi.deleteLanguage(id);
          setLanguages((prev) => prev.filter((l) => l.id !== id));
          message.success(t('common.success'));
        } catch (err) {
          message.error(getErrorMessage(err, t('common.error')));
        }
      }
    });
  };

  const handleOpenCreateCourse = () => {
    setEditingCourse(null);
    if (languages.length > 0) {
      setCourseLangId(languages[0].id);
      setCourseLevel(getDefaultLevelForLanguage(languages[0].code));
    }
    setCourseTitle(getDefaultLevelForLanguage(languages[0]?.code || 'en') + ' - ' + getDomainTitle('general', language));
    setCourseTitleEdited(false);
    setCourseDomain('general');
    setCourseDesc('');
    setIsCourseModalOpen(true);
  };

  const handleOpenEditCourse = (course: Course) => {
    setEditingCourse(course);
    setCourseLangId(course.languageId);
    setCourseTitle(course.title);
    setCourseTitleEdited(true);
    setCourseLevel(course.level);
    setCourseDomain((course.domain || 'general').toLowerCase());
    setCourseDesc(course.description || '');
    setIsCourseModalOpen(true);
  };

  const handleCourseLangChange = (langId: string) => {
    setCourseLangId(langId);
    const lang = languages.find((l) => l.id === langId);
    if (lang) {
      setCourseLevel(getDefaultLevelForLanguage(lang.code));
    }
  };

  useEffect(() => {
    if (!isCourseModalOpen || editingCourse || courseTitleEdited) return;
    setCourseTitle(`${courseLevel} - ${getDomainTitle(courseDomain, language)}`);
  }, [isCourseModalOpen, editingCourse, courseTitleEdited, courseLevel, courseDomain, language, categoryRevision]);

  const handleCreateCourse = async (e: React.FormEvent) => {
    e.preventDefault();
    if (courseTitle.trim().length < 2 || !courseLangId) {
      message.error(t('admin.missingCourseFields'));
      return;
    }
    if (courseDesc.trim().length > 2000) {
      message.error(language === 'vi' ? 'Mô tả khóa học tối đa 2000 ký tự.' : 'Course description must be at most 2000 characters.');
      return;
    }
    setIsCreatingCourse(true);
    try {
      const payload = {
        languageId: courseLangId,
        title: courseTitle.trim(),
        level: courseLevel,
        domain: courseDomain.trim().toLowerCase(),
        description: courseDesc.trim()
      };
      if (editingCourse) {
        await adminApi.updateCourse(editingCourse.id, { ...payload, status: editingCourse.status });
      } else {
        await adminApi.createCourse(payload);
      }
      message.success(t('admin.courseCreated'));
      setIsCourseModalOpen(false);
      loadCurriculumData();
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    } finally {
      setIsCreatingCourse(false);
    }
  };

  const handleDeleteCourse = (courseId: string, title: string) => {
    Modal.confirm({
      title: `Xóa khóa học "${title}"?`,
      content: 'Toàn bộ Units, bài học và từ vựng trong khóa học này sẽ bị xóa vĩnh viễn khỏi hệ thống.',
      okText: 'Xóa vĩnh viễn',
      okType: 'danger',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await adminApi.deleteCourse(courseId);
          message.success(t('admin.courseDeleted'));
          loadCurriculumData();
        } catch (err) {
          message.error(getErrorMessage(err, t('common.error')));
        }
      }
    });
  };

  const handleOpenManageCourse = async (courseId: string) => {
    setIsLoadingCourseDetail(true);
    setIsManageModalOpen(true);
    try {
      const detail = await coursesApi.getCourseDetail(courseId);
      const sortedUnits = sortUnits(detail.units || []).map((u) => ({
        ...u,
        lessons: sortLessons(u.lessons || [])
      }));
      setManagedCourse({ ...detail, units: sortedUnits });
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    } finally {
      setIsLoadingCourseDetail(false);
    }
  };

  const handleOpenPlacementCourse = async (courseId: string) => {
    try {
      const detail = await coursesApi.getCourseDetail(courseId);
      setManagedCourse(detail);
      setIsPlacementEditorOpen(true);
    } catch (error) {
      message.error(getErrorMessage(error, t('common.error')));
    }
  };

  const handleOpenSpecialPracticeCourse = async (courseId: string, section: SpecialPracticeSection) => {
    try {
      const detail = await coursesApi.getCourseDetail(courseId);
      setManagedCourse(detail);
      setSpecialPracticeSection(section);
      setIsSpecialPracticeEditorOpen(true);
    } catch (error) {
      message.error(getErrorMessage(error, t('common.error')));
    }
  };

  const reloadManagedCourse = async () => {
    if (!managedCourse) return;
    try {
      const detail = await coursesApi.getCourseDetail(managedCourse.id);
      const sortedUnits = sortUnits(detail.units || []).map((u) => ({
        ...u,
        lessons: sortLessons(u.lessons || [])
      }));
      setManagedCourse({ ...detail, units: sortedUnits });
    } catch { }
  };

  const handleCreateUnit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!managedCourse || !newUnitTitle.trim()) return;
    try {
      await adminApi.createUnit(managedCourse.id, {
        title: newUnitTitle.trim(),
        sortOrder: (managedCourse.units?.length || 0) + 1
      });
      message.success(t('admin.unitCreated'));
      setNewUnitTitle('');
      await reloadManagedCourse();
      loadCurriculumData();
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleDeleteUnit = (unitId: string) => {
    Modal.confirm({
      title: 'Xóa Unit này cùng toàn bộ bài học con?',
      okText: 'Xóa',
      okType: 'danger',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await adminApi.deleteUnit(unitId);
          message.success(t('admin.unitDeleted'));
          await reloadManagedCourse();
          loadCurriculumData();
        } catch (err) {
          message.error(getErrorMessage(err, t('common.error')));
        }
      }
    });
  };

  const handleCreateLesson = async (unitId: string) => {
    if (!newLessonTitle.trim()) return;
    try {
      await adminApi.createLesson(unitId, {
        title: newLessonTitle.trim(),
        lessonType: newLessonType,
        sortOrder: 1
      });
      message.success(t('admin.lessonCreated'));
      setNewLessonTitle('');
      setActiveUnitForLesson(null);
      await reloadManagedCourse();
      loadCurriculumData();
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleDeleteLesson = (lessonId: string) => {
    Modal.confirm({
      title: 'Xóa bài học này?',
      okText: 'Xóa bài học',
      okType: 'danger',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await adminApi.deleteLesson(lessonId);
          message.success(t('admin.lessonDeleted'));
          await reloadManagedCourse();
          loadCurriculumData();
        } catch (err) {
          message.error(getErrorMessage(err, t('common.error')));
        }
      }
    });
  };

  const handleExportCurriculum = async () => {
    try {
      const data: CurriculumExportResponse = await adminApi.exportCurriculum(
        selectedLanguageFilter || undefined
      );
      const jsonStr = JSON.stringify(data, null, 2);
      const blob = new Blob([jsonStr], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `curriculum_export_${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      message.success(t('common.success'));
    } catch (err) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleDownloadSampleJson = () => {
    message.info(t('admin.createCourseBeforeImport'));
  };

  const handleDownloadCourseTemplate = () => {
    if (!managedCourse) return;
    const courseLanguage = languages.find((item) => item.id === managedCourse.languageId);
    if (!courseLanguage) {
      message.error(t('admin.courseLanguageMissing'));
      return;
    }
    const jsonStr = JSON.stringify(createCourseMaterialTemplate(managedCourse, courseLanguage), null, 2);
    const blob = new Blob([jsonStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    const languageCode = languages.find((item) => item.id === managedCourse.languageId)?.code ?? 'en';
    link.download = `course_template_${languageCode}_${managedCourse.level}_${managedCourse.domain || 'general'}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    message.success(t('admin.templateDownloaded'));
  };

  const handleExportSingleCourse = async (courseId: string) => {
    try {
      const material = await coursesApi.getCourseMaterial(courseId);
      if (!material || Object.keys(material).length === 0) throw new Error('Khóa học chưa có nội dung để xuất.');
      const jsonStr = JSON.stringify(material, null, 2);
      const blob = new Blob([jsonStr], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `course_${courseId}_lessons_export.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      message.success(t('admin.materialExported'));
    } catch (err: any) {
      message.error(getErrorMessage(err, t('common.error')));
    }
  };

  const handleImportCourseLessons = async (jsonContent: string) => {
    if (!managedCourse) return;
    setCourseImportError('');
    try {
      const material = parseCourseMaterial(jsonContent, managedCourse);
      await adminApi.importCourseMaterial(managedCourse.id, material);
      curriculumStore.importCurriculumFromJson(JSON.stringify(material));
      examStore.upsertCourseMaterial(material);
      message.success(t('admin.materialSaved'));
      setIsCourseImportModalOpen(false);
      setCourseImportJsonText('');
      reloadManagedCourse();
      loadCurriculumData();
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      setCourseImportError(detail);
      message.error({ content: detail, duration: 8 });
    }
  };

  const handleSingleCourseFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (event) => {
      const content = event.target?.result as string;
      if (content) {
        setCourseImportJsonText(content);
      }
    };
    reader.readAsText(file);
    e.target.value = '';
  };

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (event) => {
      const content = event.target?.result as string;
      setImportJsonText(content);
    };
    reader.readAsText(file);
  };

  const handleExecuteImport = async () => {
    if (!importJsonText.trim()) return;
    setIsImporting(true);
    setImportResult(null);
    try {
      const parsed: unknown = JSON.parse(importJsonText);
      const source = Array.isArray(parsed) ? parsed : parsed && typeof parsed === 'object' && 'courses' in parsed ? parsed.courses : [parsed];
      if (!Array.isArray(source) || source.length === 0) throw new Error('JSON phải chứa ít nhất một khóa học đã tạo.');
      for (const candidate of source) {
        if (!candidate || typeof candidate !== 'object' || !('id' in candidate) || typeof candidate.id !== 'string') {
          throw new Error('Mỗi khóa học phải có ID của khóa học đã tạo trên máy chủ.');
        }
        const course = await coursesApi.getCourseDetail(candidate.id);
        const material = parseCourseMaterial(JSON.stringify(candidate), course);
        await adminApi.importCourseMaterial(course.id, material);
        curriculumStore.importCurriculumFromJson(JSON.stringify(material));
        examStore.upsertCourseMaterial(material);
      }
      setImportResult({ coursesCreated: 0, unitsCreated: 0, lessonsCreated: 0, vocabulariesCreated: 0, message: `Đã lưu nội dung ${source.length} khóa học lên máy chủ.` });
      await loadCurriculumData();
    } catch (error) {
      message.error(getErrorMessage(error, t('common.error')));
    } finally {
      setIsImporting(false);
    }
  };
  const filteredCourses = courses.filter(course =>
    (!selectedLanguageFilter || course.languageId === selectedLanguageFilter) &&
    (!selectedLevelFilter || course.level.toLowerCase() === selectedLevelFilter.toLowerCase()) &&
    (!selectedDomainFilter || (course.domain || 'general').toLowerCase() === selectedDomainFilter.toLowerCase())
  );
  const filterLanguageCode = languages.find(item => item.id === selectedLanguageFilter)?.code;
  const levelFilterOptions = filterLanguageCode
    ? getLevelsForLanguage(filterLanguageCode, language).map(item => item.code)
    : [...new Set(languages.flatMap(item => getLevelsForLanguage(item.code, language).map(level => level.code)))];
  const coursePageCount = Math.max(1, Math.ceil(filteredCourses.length / COURSE_PAGE_SIZE));
  const visibleCoursePage = Math.min(coursePage, coursePageCount);
  const visibleCourses = filteredCourses.slice((visibleCoursePage - 1) * COURSE_PAGE_SIZE, visibleCoursePage * COURSE_PAGE_SIZE);

  return (
    <div className="space-y-4">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b-2 border-[#262626] pb-3">
        <div>
          <h1 className="text-xl font-bold text-stone-900 tracking-tight">{t('admin.title')}</h1>
          <p className="text-xs text-stone-600">{t('admin.subtitle')}</p>
        </div>

        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setActiveTab('dashboard')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-1.5 ${activeTab === 'dashboard'
                ? 'bg-stone-900 text-white notebook-btn'
                : 'bg-white border-notebook text-stone-700 hover:bg-stone-100'
              }`}
          >
            <AppstoreOutlined />
            <span>{t('admin.dashboardTitle')}</span>
          </button>
          <button
            onClick={() => setActiveTab('curriculum')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-1.5 ${activeTab === 'curriculum'
                ? 'bg-stone-900 text-white notebook-btn'
                : 'bg-white border-notebook text-stone-700 hover:bg-stone-100'
              }`}
          >
            <BookOutlined />
            <span>{t('admin.curriculumTitle')}</span>
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-1.5 ${activeTab === 'users'
                ? 'bg-stone-900 text-white notebook-btn'
                : 'bg-white border-notebook text-stone-700 hover:bg-stone-100'
              }`}
          >
            <UserOutlined />
            <span>{t('admin.usersTitle')}</span>
          </button>
          <button type="button" onClick={() => setActiveTab('quality')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-1.5 ${activeTab === 'quality' ? 'bg-stone-900 text-white notebook-btn' : 'bg-white border-notebook text-stone-700 hover:bg-stone-100'}`}>
            <FileTextOutlined /> {language === 'vi' ? 'Chất lượng nội dung' : 'Content quality'}
          </button>
          <button
            onClick={() => setActiveTab('audit')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition cursor-pointer flex items-center gap-1.5 ${activeTab === 'audit'
                ? 'bg-stone-900 text-white notebook-btn'
                : 'bg-white border-notebook text-stone-700 hover:bg-stone-100'
              }`}
          >
            <SafetyCertificateOutlined />
            <span>{t('admin.auditTitle')}</span>
          </button>
        </div>
      </div>

      {activeTab === 'dashboard' && (
        <div className="space-y-4">
          {/* Executive Stats Cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
            <div className="p-4 bg-white border-notebook rounded-2xl shadow-2xs flex items-center justify-between">
              <div>
                <span className="text-[11px] font-mono font-bold text-stone-500 uppercase">{t('admin.activeLearners')}</span>
                <div className="text-2xl font-black text-stone-900 mt-0.5">
                  {users.filter((u) => u.role !== 'Admin' && u.isActive).length}
                </div>
                <span className="text-[10px] text-emerald-600 font-bold">● {t('admin.activeNow')}</span>
              </div>
              <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center text-lg font-bold">
                <UserOutlined />
              </div>
            </div>

            <div className="p-4 bg-white border-notebook rounded-2xl shadow-2xs flex items-center justify-between">
              <div>
                <span className="text-[11px] font-mono font-bold text-stone-500 uppercase">{t('admin.trainingLanguages')}</span>
                <div className="text-2xl font-black text-stone-900 mt-0.5">
                  {languages.length}
                </div>
                <span className="text-[10px] text-stone-500 font-mono">{languages.map((item) => item.code.toUpperCase()).join(', ')}</span>
              </div>
              <div className="w-10 h-10 rounded-xl bg-amber-50 border border-amber-200 text-amber-700 flex items-center justify-center text-lg font-bold">
                <GlobalOutlined />
              </div>
            </div>

            <div className="p-4 bg-white border-notebook rounded-2xl shadow-2xs flex items-center justify-between">
              <div>
                <span className="text-[11px] font-mono font-bold text-stone-500 uppercase">{t('admin.availableCourses')}</span>
                <div className="text-2xl font-black text-stone-900 mt-0.5">
                  {courses.length}
                </div>
                <span className="text-[10px] text-stone-500 font-mono">{t('admin.published')}</span>
              </div>
              <div className="w-10 h-10 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-700 flex items-center justify-center text-lg font-bold">
                <BookOutlined />
              </div>
            </div>

            <div className="p-4 bg-white border-notebook rounded-2xl shadow-2xs flex items-center justify-between">
              <div>
                <span className="text-[11px] font-mono font-bold text-stone-500 uppercase">{t('admin.securityLogs')}</span>
                <div className="text-2xl font-black text-stone-900 mt-0.5">
                  {auditLogs.length || 0}
                </div>
                <span className="text-[10px] text-rose-600 font-bold">● {t('admin.recordedActivity')}</span>
              </div>
              <div className="w-10 h-10 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 flex items-center justify-center text-lg font-bold">
                <SafetyCertificateOutlined />
              </div>
            </div>
          </div>

          {/* Quick Action Hub & System Specs */}
          <div className="grid grid-cols-1 gap-4">
            <div className="p-5 bg-white border-notebook rounded-2xl shadow-2xs space-y-4">
              <h3 className="text-sm font-extrabold text-stone-900 flex items-center gap-2">
                <ThunderboltOutlined className="text-amber-500" />
                <span>{t('admin.quickActions')}</span>
              </h3>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
                <button
                  onClick={() => setIsLanguageModalOpen(true)}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <PlusOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{t('admin.addLanguage')}</span>
                  <span className="text-[10px] text-stone-500 block">{t('admin.addLanguageHint')}</span>
                </button>
                <button
                  onClick={() => { setActiveTab('curriculum'); handleOpenCreateCourse(); }}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <PlusOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{t('admin.addCourse')}</span>
                  <span className="text-[10px] text-stone-500 block">{t('admin.createCourseFirst')}</span>
                </button>
                <button
                  onClick={handleExportCurriculum}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <DownloadOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{t('admin.exportData')}</span>
                  <span className="text-[10px] text-stone-500 block">{t('admin.exportDataHint')}</span>
                </button>
                <button
                  onClick={() => setActiveTab('curriculum')}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <BookOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{t('admin.manageLessons')}</span>
                  <span className="text-[10px] text-stone-500 block">{t('admin.manageLessonsHint')}</span>
                </button>
                <button
                  onClick={() => setActiveTab('users')}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <UserOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{t('admin.manageAccounts')}</span>
                  <span className="text-[10px] text-stone-500 block">{t('admin.manageAccountsHint')}</span>
                </button>
                <button
                  onClick={() => setActiveTab('audit')}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <SafetyCertificateOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{t('admin.securityAudit')}</span>
                  <span className="text-[10px] text-stone-500 block">{t('admin.securityAuditHint')}</span>
                </button>
                <button
                  onClick={() => setIsPlacementReviewOpen(true)}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer"
                >
                  <AuditOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{language === 'vi' ? 'Chấm bài đầu vào' : 'Review placement tests'}</span>
                  <span className="text-[10px] text-stone-500 block">{language === 'vi' ? 'Nghe bài nói và chấm bài viết' : 'Listen to speech and grade writing'}</span>
                </button>
                <button onClick={() => setIsSpecialPracticeReviewOpen(true)}
                  className="p-3 bg-[#FAF6EE] hover:bg-stone-100 border-notebook rounded-xl text-left transition cursor-pointer">
                  <AuditOutlined className="text-stone-700 mb-1 inline-flex" />
                  <span className="text-xs font-bold text-stone-900 block">{language === 'vi' ? 'Chấm luyện thi & phỏng vấn' : 'Review exams & interviews'}</span>
                  <span className="text-[10px] text-stone-500 block">{language === 'vi' ? 'Chấm câu tự luận theo rubric' : 'Score open responses using the rubric'}</span>
                </button>
              </div>
            </div>
          </div>

          {/* Recent Activity Log Preview */}
          <div className="p-5 bg-white border-notebook rounded-2xl shadow-2xs space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-extrabold text-stone-900">{t('admin.recentActivity')}</h3>
              <button
                onClick={() => setActiveTab('audit')}
                className="text-xs font-bold text-stone-600 hover:text-stone-900 cursor-pointer"
              >
                {t('admin.viewAllLogs')} &rarr;
              </button>
            </div>
            {auditLogs.length === 0 ? (
              <div className="text-center py-6 text-xs text-stone-500">{t('admin.noAuditLogs')}</div>
            ) : (
              <div className="divide-y divide-stone-100 text-xs">
                {auditLogs.slice(0, 5).map((log) => (
                  <div key={log.id} className="py-2.5 flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-bold text-stone-900 bg-stone-100 px-1.5 py-0.5 rounded">
                        {log.action}
                      </span>
                      <span className="text-stone-600">{log.details || log.targetEntity}</span>
                    </div>
                    <span className="text-[10px] font-mono text-stone-400">
                      {new Date(log.createdAt).toLocaleTimeString()} {new Date(log.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'quality' && <><ContentQualityPanel courses={courses} /><SrsEvaluationPanel /></>}

      {activeTab === 'curriculum' && (
        <div className="space-y-4">
          <div className="p-4 bg-white border-notebook rounded-2xl shadow-2xs space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-stone-200 pb-3">
              <div className="flex items-center gap-2">
                <GlobalOutlined className="text-amber-600 text-base" />
                <h3 className="text-xs font-bold uppercase tracking-wider text-stone-800">
                  {t('admin.languagesManagement')} ({languages.length})
                </h3>
              </div>

              <div className="flex items-center gap-2">
                <button type="button" onClick={() => setIsCategoryManagerOpen(true)}
                  className="rounded-lg border-notebook bg-white px-3 py-1.5 text-xs font-bold">
                  {language === 'vi' ? 'Quản lý trình độ & lĩnh vực' : 'Manage levels & domains'}
                </button>
                <button
                  type="button"
                  onClick={() => setIsLanguageModalOpen(true)}
                  className="px-3 py-1.5 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-xs font-bold notebook-btn flex items-center gap-1.5 transition cursor-pointer"
                >
                  <PlusOutlined />
                  <span>{t('admin.addLanguage')}</span>
                </button>
                <button
                  type="button"
                  onClick={handleExportCurriculum}
                  className="px-3 py-1.5 bg-white hover:bg-stone-100 text-stone-800 border-notebook rounded-lg text-xs font-bold flex items-center gap-1.5 transition cursor-pointer"
                  title="Export Curriculum as JSON"
                >
                  <DownloadOutlined className="text-blue-600" />
                  <span>{t('admin.exportJson')}</span>
                </button>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-2">
              {languages.map((lang) => (
                <div
                  key={lang.id || lang.code}
                  className="p-2.5 bg-[#FAF6EE] border border-stone-300 rounded-xl flex items-center justify-between"
                >
                  <div className="flex items-center gap-2 truncate">
                    <span className="text-xl">{lang.flagEmoji || '🌐'}</span>
                    <div className="truncate">
                      <span className="text-xs font-bold text-stone-900 block truncate">
                        {lang.name}
                      </span>
                      <span className="text-[10px] text-stone-500 font-mono block">
                        {lang.code.toUpperCase()}
                      </span>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleDeleteLanguage(lang.id)}
                    className="p-1 text-stone-400 hover:text-rose-600 transition cursor-pointer"
                    title={t('common.delete')}
                  >
                    <DeleteOutlined className="text-xs" />
                  </button>
                </div>
              ))}
            </div>
          </div>

          <div className="p-4 bg-white border-notebook rounded-2xl shadow-2xs space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-stone-200 pb-2">
              <h3 className="text-xs font-bold uppercase tracking-wider text-stone-800">
                {t('admin.coursesAndLessons')} ({courses.length})
              </h3>

              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  onClick={handleOpenCreateCourse}
                  className="px-3 py-1 bg-amber-400 hover:bg-amber-500 text-stone-900 border-notebook rounded-lg text-xs font-bold flex items-center gap-1.5 transition cursor-pointer"
                >
                  <PlusOutlined />
                  <span>{t('admin.addCourse')}</span>
                </button>

                <NotebookSelect
                  value={selectedLanguageFilter}
                  onChange={(val) => { setSelectedLanguageFilter(val); setSelectedLevelFilter(''); setCoursePage(1); }}
                  options={[
                    { value: '', label: t('admin.allLanguages') },
                    ...languages.map((l) => ({
                      value: l.id,
                      label: `${l.flagEmoji || ''} ${l.code === 'en' ? t('common.english') : l.code === 'vi' ? t('common.vietnamese') : l.name}`
                    }))
                  ]}
                  className="w-40"
                />

                <NotebookSelect
                  value={selectedLevelFilter}
                  onChange={(val) => { setSelectedLevelFilter(val); setCoursePage(1); }}
                  options={[
                    { value: '', label: t('admin.allLevels') },
                    ...levelFilterOptions.map((level) => ({ value: level, label: level }))
                  ]}
                  className="w-36"
                />

                <NotebookSelect
                  value={selectedDomainFilter}
                  onChange={(val) => { setSelectedDomainFilter(val); setCoursePage(1); }}
                  options={[
                    { value: '', label: t('admin.allDomains') },
                    ...DOMAINS.map(domain => ({ value: domain.code, label: getDomainTitle(domain.code, language) }))
                  ]}
                  className="w-48"
                />
              </div>
            </div>

            <div className="overflow-x-auto border-notebook rounded-xl">
              <table className="w-full text-left text-xs">
                <thead className="bg-stone-100 text-stone-600 font-bold border-b border-stone-200">
                  <tr>
                    <th className="p-3">{t('admin.courseName')}</th>
                    <th className="p-3">{t('courses.level')}</th>
                    <th className="p-3">{t('admin.domain')}</th>
                    <th className="p-3">{t('courses.units')}</th>
                    <th className="p-3">{t('courses.lessons')}</th>
                    <th className="p-3">{t('admin.status')}</th>
                    <th className="p-3 text-right">{t('admin.actions')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100">
                  {visibleCourses.map((c) => (
                    <tr key={c.id} className="hover:bg-stone-50/60">
                      <td className="p-3 font-bold text-stone-900">{c.title}</td>
                      <td className="p-3 font-mono font-bold text-indigo-700">{c.level}</td>
                      <td className="p-3 font-mono text-[11px] uppercase text-emerald-700">
                        {getDomainTitle(c.domain || 'general', language)}
                      </td>
                      <td className="p-3 font-mono text-stone-600">{c.unitsCount}</td>
                      <td className="p-3 font-mono text-stone-600">{c.totalLessons}</td>
                      <td className="p-3">
                        <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-50 text-emerald-800 border border-emerald-200 font-bold">
                          {t(c.status === 'Published' ? 'admin.published' : 'admin.draft')}
                        </span>
                      </td>
                      <td className="p-3 text-right whitespace-nowrap">
                        <div className="inline-flex items-center justify-end gap-1.5">
                          <button type="button" onClick={() => void handleOpenManageCourse(c.id)}
                            className="rounded-lg border border-stone-300 bg-white px-2.5 py-1.5 text-[11px] font-bold hover:bg-stone-100">
                            <FolderOpenOutlined className="mr-1 text-amber-600" />{t('admin.courseDetails')}
                          </button>
                          <button type="button" onClick={() => void handleOpenSpecialPracticeCourse(c.id, 'IELTS')}
                            className="rounded-lg border border-amber-400 bg-amber-100 px-2.5 py-1.5 text-[11px] font-bold hover:bg-amber-200">
                            IELTS
                          </button>
                          <button type="button" onClick={() => void handleOpenSpecialPracticeCourse(c.id, 'TOEIC')}
                            className="rounded-lg border border-amber-400 bg-amber-100 px-2.5 py-1.5 text-[11px] font-bold hover:bg-amber-200">
                            TOEIC
                          </button>
                          <Dropdown trigger={['click']} menu={{ items: [
                            { key: 'edit', label: t('admin.editCourse') },
                            { key: 'placement', label: language === 'vi' ? `Đề đầu vào ${c.level}` : `${c.level} placement test` },
                            { key: 'interview', label: language === 'vi' ? 'Phỏng vấn' : 'Interviews' },
                            { type: 'divider' },
                            { key: 'delete', label: t('admin.deleteCourse'), danger: true }
                          ], onClick: ({ key }) => {
                            if (key === 'edit') handleOpenEditCourse(c);
                            if (key === 'placement') void handleOpenPlacementCourse(c.id);
                            if (key === 'interview') void handleOpenSpecialPracticeCourse(c.id, 'interview');
                            if (key === 'delete') handleDeleteCourse(c.id, c.title);
                          } }}>
                            <button type="button" aria-label={`${t('admin.actions')}: ${c.title}`}
                              className="rounded-lg border border-stone-300 bg-white px-2.5 py-1.5 text-[11px] font-bold hover:bg-stone-100">
                              <MoreOutlined /> <span className="sr-only">{t('admin.actions')}</span>
                            </button>
                          </Dropdown>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {filteredCourses.length === 0 && (
                    <tr>
                      <td colSpan={7} className="p-6 text-center text-xs text-stone-500">
                        {t('admin.noCoursesFound')}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            {filteredCourses.length > 0 && <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
              <p className="text-xs text-stone-600">
                {language === 'vi'
                  ? `Hiển thị ${filteredCourses.length ? (visibleCoursePage - 1) * COURSE_PAGE_SIZE + 1 : 0}–${Math.min(visibleCoursePage * COURSE_PAGE_SIZE, filteredCourses.length)} / ${filteredCourses.length} khóa học`
                  : `Showing ${filteredCourses.length ? (visibleCoursePage - 1) * COURSE_PAGE_SIZE + 1 : 0}–${Math.min(visibleCoursePage * COURSE_PAGE_SIZE, filteredCourses.length)} of ${filteredCourses.length} courses`}
              </p>
              <Pagination page={visibleCoursePage} pageCount={coursePageCount} onChange={setCoursePage} />
            </div>}
          </div>
        </div>
      )}

      {activeTab === 'users' && (
        <div className="bg-white border-notebook-2 rounded-2xl p-5 shadow-xs space-y-4">
          <div className="flex gap-2">
            <div className="relative flex-1">
              <SearchOutlined className="absolute left-3 top-3 text-stone-400" />
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder={t('admin.searchUsers')}
                className="w-full pl-9 pr-3 py-2 bg-stone-50 border-notebook rounded-lg text-xs font-medium focus:outline-none focus:ring-2 focus:ring-amber-400"
              />
            </div>
            <button
              onClick={() => loadUsers(searchTerm)}
              className="px-4 py-2 bg-stone-900 text-white rounded-lg text-xs font-bold notebook-btn hover:bg-stone-800 transition cursor-pointer"
            >
              {t('common.all')}
            </button>
          </div>

          {isLoading ? (
            <div className="py-12 text-center">
              <LoadingOutlined className="text-2xl text-stone-500 animate-spin" />
            </div>
          ) : (
            <div className="overflow-x-auto border-notebook rounded-xl">
              <table className="w-full text-left text-xs">
                <thead className="bg-stone-100 text-stone-600 font-bold border-b border-stone-200">
                  <tr>
                    <th className="p-3">{t('admin.userCol')}</th>
                    <th className="p-3">{t('admin.role')}</th>
                    <th className="p-3">{t('admin.status')}</th>
                    <th className="p-3 text-right">{t('decks.actions')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100">
                  {users.map((u) => (
                    <tr key={u.id} className="hover:bg-stone-50/60">
                      <td className="p-3">
                        <span className="font-bold text-stone-900 block">{u.displayName}</span>
                        <span className="text-[11px] text-stone-500 block font-mono">{u.email}</span>
                      </td>
                      <td className="p-3">
                        <span
                          className={`font-mono font-bold text-[10px] px-2 py-0.5 rounded border ${u.role === 'Admin'
                              ? 'bg-rose-100 text-rose-800 border-rose-300'
                              : 'bg-stone-100 text-stone-700 border-stone-300'
                            }`}
                        >
                          {u.role}
                        </span>
                      </td>
                      <td className="p-3">
                        <span
                          className={`text-[10px] font-bold px-2 py-0.5 rounded border ${u.isActive
                              ? 'bg-emerald-50 text-emerald-700 border-emerald-300'
                              : 'bg-rose-50 text-rose-700 border-rose-300'
                            }`}
                        >
                          {u.isActive ? t('admin.active') : t('admin.disabled')}
                        </span>
                      </td>
                      <td className="p-3 text-right space-x-2">
                        <button
                          onClick={() => handleToggleRole(u)}
                          className="px-2 py-1 rounded border border-stone-300 hover:bg-stone-100 text-[10px] font-bold cursor-pointer"
                        >
                          {u.role === 'Admin' ? t('admin.demoteToLearner') : t('admin.promoteToAdmin')}
                        </button>
                        <button
                          onClick={() => handleToggleStatus(u)}
                          className={`px-2 py-1 rounded border text-[10px] font-bold cursor-pointer ${u.isActive
                              ? 'border-rose-300 text-rose-700 hover:bg-rose-50'
                              : 'border-emerald-300 text-emerald-700 hover:bg-emerald-50'
                            }`}
                        >
                          {u.isActive ? <LockOutlined className="mr-1" /> : <UnlockOutlined className="mr-1" />}
                          {u.isActive ? t('admin.disable') : t('admin.activate')}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeTab === 'audit' && (
        <div className="bg-white border-notebook-2 rounded-2xl p-5 shadow-xs space-y-4">
          <div className="overflow-x-auto border-notebook rounded-xl">
            <table className="w-full text-left text-xs">
              <thead className="bg-stone-100 text-stone-600 font-bold border-b border-stone-200">
                <tr>
                  <th className="p-3">{t('admin.auditAction')}</th>
                  <th className="p-3">{t('admin.auditTarget')}</th>
                  <th className="p-3">{t('admin.auditDetails')}</th>
                  <th className="p-3">{t('admin.auditIp')}</th>
                  <th className="p-3">{t('admin.auditDate')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100 font-mono text-[11px]">
                {auditLogs.map((log) => (
                  <tr key={log.id} className="hover:bg-stone-50/60">
                    <td className="p-3 font-bold text-stone-900">{log.action}</td>
                    <td className="p-3 text-stone-600">{log.targetEntity}</td>
                    <td className="p-3 text-stone-800 font-sans">{log.details}</td>
                    <td className="p-3 text-stone-500">{log.ipAddress}</td>
                    <td className="p-3 text-stone-500">
                      {new Date(log.createdAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {isLanguageModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs">
          <form
            onSubmit={handleCreateLanguage}
            className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-md w-full p-5 space-y-4"
          >
            <div className="flex items-center justify-between border-b-2 border-[#262626] pb-3">
              <h3 className="text-sm font-bold text-stone-900">{t('admin.addLanguageTitle')}</h3>
              <button
                type="button"
                onClick={() => setIsLanguageModalOpen(false)}
                className="p-1 text-stone-500 hover:text-stone-900 cursor-pointer"
              >
                <CloseOutlined />
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div>
                <label className="block font-bold text-stone-800 mb-1">{t('admin.langCode')}</label>
                <input
                  type="text"
                  required
                  placeholder="en, ja, ko, de, fr..."
                  value={newLangCode}
                  onChange={(e) => setNewLangCode(e.target.value)}
                  className="w-full p-2 bg-white border border-stone-300 rounded-lg font-mono outline-none"
                />
              </div>

              <div>
                <label className="block font-bold text-stone-800 mb-1">{t('admin.langName')}</label>
                <input
                  type="text"
                  required
                  placeholder="English, Japanese, Vietnamese..."
                  value={newLangName}
                  onChange={(e) => setNewLangName(e.target.value)}
                  className="w-full p-2 bg-white border border-stone-300 rounded-lg outline-none"
                />
              </div>

              <div>
                <label className="block font-bold text-stone-800 mb-1">{t('admin.langNative')}</label>
                <input
                  type="text"
                  placeholder="English, 日本語, Tiếng Việt..."
                  value={newLangNative}
                  onChange={(e) => setNewLangNative(e.target.value)}
                  className="w-full p-2 bg-white border border-stone-300 rounded-lg outline-none"
                />
              </div>

              <div>
                <label className="block font-bold text-stone-800 mb-1">{t('admin.langFlagEmoji')}</label>
                <input
                  type="text"
                  placeholder="🇬🇧, 🇯🇵, 🇰🇷, 🇻🇳..."
                  value={newLangFlag}
                  onChange={(e) => setNewLangFlag(e.target.value)}
                  className="w-full p-2 bg-white border border-stone-300 rounded-lg outline-none text-base"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-stone-200">
              <button
                type="button"
                onClick={() => setIsLanguageModalOpen(false)}
                className="px-3 py-1.5 rounded-lg border border-stone-300 bg-white hover:bg-stone-100 text-xs font-bold cursor-pointer"
              >
                {t('common.cancel')}
              </button>
              <button
                type="submit"
                className="px-4 py-1.5 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn cursor-pointer"
              >
                {t('admin.saveLanguage')}
              </button>
            </div>
          </form>
        </div>
      )}

      {isImportModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs overflow-y-auto">
          <div className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-2xl w-full p-6 space-y-4 my-8">
            <div className="flex items-center justify-between border-b-2 border-[#262626] pb-3">
              <div className="flex items-center gap-2">
                <UploadOutlined className="text-amber-700 text-lg" />
                <h3 className="text-sm font-bold text-stone-900">{t('admin.importCurriculumTitle')}</h3>
              </div>
              <button
                type="button"
                onClick={() => {
                  setIsImportModalOpen(false);
                  setImportResult(null);
                }}
                className="p-1 text-stone-500 hover:text-stone-900 cursor-pointer"
              >
                <CloseOutlined />
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="flex items-center justify-between">
                <p className="text-stone-600">{t('admin.importCurriculumHint')}</p>
                <button
                  type="button"
                  onClick={handleDownloadSampleJson}
                  className="text-indigo-700 hover:text-indigo-900 font-bold flex items-center gap-1 cursor-pointer"
                >
                  <FileTextOutlined />
                  <span>{t('admin.downloadSampleTemplate')}</span>
                </button>
              </div>

              <div className="flex items-center gap-3">
                <input
                  type="file"
                  ref={fileInputRef}
                  accept=".json"
                  onChange={handleFileUpload}
                  className="hidden"
                />
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="px-3 py-1.5 bg-white border-notebook rounded-lg text-xs font-bold hover:bg-stone-100 flex items-center gap-1.5 cursor-pointer"
                >
                  <UploadOutlined />
                  <span>{t('admin.chooseJsonFile')}</span>
                </button>
                <label className="flex items-center gap-1.5 cursor-pointer font-bold text-stone-700">
                  <input
                    type="checkbox"
                    checked={importReplace}
                    onChange={(e) => setImportReplace(e.target.checked)}
                  />
                  <span>{t('admin.replaceExistingData')}</span>
                </label>
              </div>

              <div>
                <label className="block font-bold text-stone-800 mb-1">JSON Content</label>
                <textarea
                  rows={8}
                  value={importJsonText}
                  onChange={(e) => setImportJsonText(e.target.value)}
                  placeholder={t('admin.jsonPlaceholder')}
                  className="w-full p-2.5 font-mono text-xs bg-white border border-stone-300 rounded-lg outline-none"
                />
              </div>

              {importResult && (
                <div className="p-3 bg-emerald-50 border border-emerald-300 rounded-xl space-y-1 text-emerald-900">
                  <div className="font-bold flex items-center gap-1.5">
                    <CheckCircleOutlined className="text-emerald-600" />
                    <span>{importResult.message}</span>
                  </div>
                  <div className="text-[11px] font-mono">
                    Courses: {importResult.coursesCreated} • Units: {importResult.unitsCreated} • Lessons: {importResult.lessonsCreated} • Vocabs: {importResult.vocabulariesCreated}
                  </div>
                </div>
              )}
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-stone-200">
              <button
                type="button"
                onClick={() => {
                  setIsImportModalOpen(false);
                  setImportResult(null);
                }}
                className="px-3.5 py-1.5 rounded-lg border border-stone-300 bg-white hover:bg-stone-100 text-xs font-bold cursor-pointer"
              >
                {t('common.close')}
              </button>
              <button
                type="button"
                disabled={!importJsonText.trim() || isImporting}
                onClick={handleExecuteImport}
                className="px-4 py-1.5 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn flex items-center gap-1.5 disabled:opacity-50 cursor-pointer"
              >
                {isImporting && <LoadingOutlined className="animate-spin" />}
                <span>{t('admin.startImport')}</span>
              </button>
            </div>
          </div>
        </div>
      )}
      {/* Modal: Thêm Khóa học Mới */}
      {isCourseModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs overflow-y-auto">
          <div className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-xl w-full p-6 space-y-4 my-8">
            <div className="flex items-center justify-between border-b-2 border-[#262626] pb-3">
              <div className="flex items-center gap-2">
                <BookOutlined className="text-amber-600 text-lg" />
                <h3 className="text-sm font-bold text-stone-900">Thêm Khóa Học Mới</h3>
              </div>
              <button
                type="button"
                onClick={() => setIsCourseModalOpen(false)}
                className="p-1 text-stone-500 hover:text-stone-900 cursor-pointer"
              >
                <CloseOutlined />
              </button>
            </div>

            <form onSubmit={handleCreateCourse} className="space-y-3.5 text-xs">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {/* Language Select */}
                <div>
                  <label className="block font-bold text-stone-800 mb-1">
                    {language === 'vi' ? 'Ngôn ngữ đào tạo *' : 'Target Language *'}
                  </label>
                  <NotebookSelect
                    value={courseLangId}
                    onChange={(val) => handleCourseLangChange(val)}
                    disabled={Boolean(editingCourse)}
                    options={languages.map((l) => ({
                      value: l.id,
                      label: `${l.flagEmoji} ${l.name} (${l.code.toUpperCase()})`
                    }))}
                    className="w-full"
                  />
                </div>

                {/* Level Select (adapted to language) */}
                <div>
                  <label className="block font-bold text-stone-800 mb-1">
                    {language === 'vi' ? 'Trình độ' : 'Proficiency Level'} ({languages.find((l) => l.id === courseLangId)?.code.toUpperCase() || 'EN'})
                  </label>
                  <NotebookSelect
                    value={courseLevel}
                    onChange={(val) => setCourseLevel(val)}
                    options={getLevelsForLanguage(
                      languages.find((l) => l.id === courseLangId)?.code || 'en',
                      language
                    ).map((lvl) => ({
                      value: lvl.code,
                      label: `${lvl.code} - ${lvl.title}`
                    }))}
                    className="w-full"
                  />
                </div>
              </div>

              {/* Domain / Field Select */}
              <div>
                <label className="block font-bold text-stone-800 mb-1">
                  {language === 'vi' ? 'Lĩnh vực / Chuyên ngành *' : 'Field / Domain *'}
                </label>
                <NotebookSelect
                  value={courseDomain}
                  onChange={(val) => setCourseDomain(val)}
                  options={DOMAINS.map((d) => ({
                    value: d.code,
                    label: `${getDomainTitle(d.code, language)} — ${language === 'vi' ? d.desc : (d.descEn || d.desc)}`
                  }))}
                  className="w-full"
                />
              </div>

              {/* Course Title */}
              <div>
                <label className="block font-bold text-stone-800 mb-1">{language === 'vi' ? 'Tiêu đề khóa học *' : 'Course title *'}</label>
                <input
                  type="text"
                  required
                  minLength={2}
                  maxLength={200}
                  value={courseTitle}
                  onChange={(e) => { setCourseTitle(e.target.value); setCourseTitleEdited(true); }}
                  placeholder={language === 'vi' ? 'Ví dụ: Tiếng Nhật Thương Mại & Phỏng vấn N3...' : 'Example: Business Japanese and N3 interviews...'}
                  className="w-full p-2 bg-white border border-stone-300 rounded-lg outline-none font-medium"
                />
              </div>

              {/* Description */}
              <div>
                <label className="block font-bold text-stone-800 mb-1">{language === 'vi' ? 'Mô tả tóm tắt' : 'Short description'}</label>
                <textarea
                  rows={3}
                  maxLength={2000}
                  value={courseDesc}
                  onChange={(e) => setCourseDesc(e.target.value)}
                  placeholder={language === 'vi' ? 'Giáo trình tập trung vào đàm phán hợp đồng, văn hóa doanh nghiệp...' : 'The course focuses on contract negotiations and workplace culture...'}
                  className="w-full p-2 bg-white border border-stone-300 rounded-lg outline-none"
                />
              </div>

              <div className="flex justify-end gap-2 pt-3 border-t border-stone-200">
                <button
                  type="button"
                  onClick={() => setIsCourseModalOpen(false)}
                  className="px-3.5 py-1.5 rounded-lg border border-stone-300 bg-white hover:bg-stone-100 text-xs font-bold cursor-pointer"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={isCreatingCourse}
                  className="px-4 py-1.5 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
                >
                  {isCreatingCourse && <LoadingOutlined className="animate-spin" />}
                  <PlusOutlined />
                  <span>{language === 'vi' ? 'Lưu khóa học' : 'Save course'}</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Quản lý Units & Bài học chi tiết */}
      {isManageModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs overflow-y-auto">
          <div className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-3xl w-full p-6 space-y-4 my-8">
            <div className="flex items-center justify-between border-b-2 border-[#262626] pb-3">
              <div>
                <div className="flex items-center gap-2">
                  <FolderOpenOutlined className="text-amber-700 text-lg" />
                  <h3 className="text-sm font-bold text-stone-900">
                    Quản lý Chi tiết: {managedCourse?.title || 'Đang tải...'}
                  </h3>
                </div>
                {managedCourse && (
                  <div className="flex items-center gap-2 mt-0.5 text-[11px] text-stone-500 font-mono">
                    <span className="font-bold text-indigo-700">{managedCourse.level}</span>
                    <span>•</span>
                    <span className="uppercase font-bold text-emerald-700">{managedCourse.domain}</span>
                    <span>•</span>
                    <span>{managedCourse.units?.length || 0} Units</span>
                  </div>
                )}
              </div>
              <button
                type="button"
                onClick={() => setIsManageModalOpen(false)}
                className="p-1 text-stone-500 hover:text-stone-900 cursor-pointer"
              >
                <CloseOutlined />
              </button>
            </div>

            {isLoadingCourseDetail ? (
              <div className="py-12 text-center">
                <LoadingOutlined className="text-3xl text-stone-500 animate-spin" />
              </div>
            ) : managedCourse ? (
              <div className="space-y-4 max-h-[68vh] overflow-y-auto pr-1 text-xs">
                {/* THANH CÔNG CỤ NHẬP / XUẤT BÀI HỌC DÀNH RIÊNG CHO KHÓA HỌC NÀY */}
                <div className="p-3 bg-amber-50/90 border border-amber-300 rounded-xl flex flex-wrap items-center justify-between gap-2.5 shadow-2xs">
                  <div className="flex items-center gap-2">
                    <ThunderboltOutlined className="text-amber-600 text-sm" />
                    <div>
                      <div className="font-bold text-amber-950 text-xs">
                        Dữ liệu Bài học & Kỹ năng Chuyên sâu ({managedCourse.level} • {(managedCourse.domain ?? 'general').toUpperCase()})
                      </div>
                      <div className="text-[10px] text-stone-600">
                        File JSON tích hợp đầy đủ 6 kỹ năng: Từ vựng, Ngữ pháp, Nghe, Nói, Đọc, Viết & Quiz
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setIsPlacementEditorOpen(true)}
                      className="px-2.5 py-1.5 bg-white border border-stone-300 rounded-lg text-[11px] font-bold cursor-pointer"
                    >Quản lý đề đầu vào 4 kỹ năng</button>
                    {(['IELTS', 'TOEIC', 'interview'] as const).map(section => <button key={section} type="button" onClick={() => { setSpecialPracticeSection(section); setIsSpecialPracticeEditorOpen(true); }}
                      className="px-2.5 py-1.5 bg-white border border-stone-300 rounded-lg text-[11px] font-bold cursor-pointer">
                      {section === 'interview' ? (language === 'vi' ? 'Phỏng vấn' : 'Interviews') : section}
                    </button>)}
                    <button
                      type="button"
                      onClick={handleDownloadCourseTemplate}
                      className="px-2.5 py-1.5 bg-white hover:bg-stone-50 border border-stone-300 rounded-lg text-[11px] font-bold text-stone-800 flex items-center gap-1.5 cursor-pointer shadow-2xs"
                      title="Tải template JSON gồm đủ 6 kỹ năng chuẩn theo Ngôn ngữ, Trình độ và Chuyên ngành của khóa học này"
                    >
                      <DownloadOutlined className="text-amber-600" />
                      <span>Tải JSON Mẫu Khóa học</span>
                    </button>

                    <button
                      type="button"
                      onClick={() => setIsCourseImportModalOpen(true)}
                      className="px-2.5 py-1.5 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-[11px] font-bold flex items-center gap-1.5 cursor-pointer notebook-btn shadow-2xs"
                      title="Nhập các Unit và Lesson từ file JSON vào đúng khóa học này"
                    >
                      <UploadOutlined />
                      <span>Nhập Bài học JSON</span>
                    </button>

                    <button
                      type="button"
                      onClick={() => handleExportSingleCourse(managedCourse.id)}
                      className="px-2.5 py-1.5 bg-white hover:bg-stone-50 border border-stone-300 rounded-lg text-[11px] font-bold text-stone-800 flex items-center gap-1.5 cursor-pointer shadow-2xs"
                      title="Xuất toàn bộ bài học của khóa học này ra file JSON"
                    >
                      <FileTextOutlined className="text-blue-600" />
                      <span>Xuất Dữ liệu Khóa</span>
                    </button>
                  </div>
                </div>

                {/* Form thêm Unit mới */}
                <form
                  onSubmit={handleCreateUnit}
                  className="p-3 bg-white border border-stone-200 rounded-xl flex items-center gap-2 shadow-2xs"
                >
                  <FolderOutlined className="text-stone-400 text-sm" />
                  <input
                    type="text"
                    required
                    value={newUnitTitle}
                    onChange={(e) => setNewUnitTitle(e.target.value)}
                    placeholder="Nhập tên Unit mới (ví dụ: Unit 01 — Bảng chữ cái & Chào hỏi)..."
                    className="flex-1 p-1.5 text-xs outline-none bg-stone-50 rounded border border-stone-200"
                  />
                  <button
                    type="submit"
                    className="px-3 py-1.5 bg-stone-900 text-white rounded text-xs font-bold notebook-btn shrink-0 cursor-pointer flex items-center gap-1"
                  >
                    <PlusOutlined />
                    <span>Thêm Unit</span>
                  </button>
                </form>

                {/* Danh sách Units & Lessons */}
                <div className="space-y-3">
                  {managedCourse.units?.length === 0 ? (
                    <div className="p-8 text-center text-stone-400 bg-white rounded-xl border border-dashed border-stone-300">
                      Khóa học chưa có Unit nào. Hãy nhập tên và bấm "Thêm Unit" ở trên!
                    </div>
                  ) : (
                    managedCourse.units.map((unit) => (
                      <div
                        key={unit.id}
                        className="bg-white border-notebook rounded-xl p-3.5 space-y-2.5 shadow-2xs"
                      >
                        {/* Unit Header */}
                        <div className="flex items-center justify-between pb-2 border-b border-stone-100">
                          <div className="flex items-center gap-2 truncate">
                            <span className="font-bold text-xs text-stone-900 truncate">
                              📁 {unit.title}
                            </span>
                            <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-stone-100 text-stone-600">
                              {unit.lessons?.length || 0} bài học
                            </span>
                          </div>

                          <div className="flex items-center gap-1.5">
                            <button
                              type="button"
                              onClick={() => {
                                if (activeUnitForLesson === unit.id) {
                                  setActiveUnitForLesson(null);
                                } else {
                                  setActiveUnitForLesson(unit.id);
                                  setNewLessonTitle('');
                                }
                              }}
                              className="px-2 py-0.5 rounded text-[11px] font-bold bg-amber-100 hover:bg-amber-200 text-amber-900 border border-amber-300 transition cursor-pointer flex items-center gap-1"
                            >
                              <PlusOutlined />
                              <span>Thêm bài học</span>
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDeleteUnit(unit.id)}
                              className="p-1 text-stone-400 hover:text-rose-600 transition cursor-pointer"
                              title="Xóa Unit này"
                            >
                              <DeleteOutlined className="text-xs" />
                            </button>
                          </div>
                        </div>

                        {/* Form thêm bài học vào Unit này */}
                        {activeUnitForLesson === unit.id && (
                          <div className="p-2.5 bg-amber-50/70 border border-amber-200 rounded-lg flex flex-wrap items-center gap-2">
                            <input
                              type="text"
                              value={newLessonTitle}
                              onChange={(e) => setNewLessonTitle(e.target.value)}
                              placeholder="Tiêu đề bài học mới..."
                              className="flex-1 min-w-[200px] p-1.5 bg-white border border-stone-300 rounded text-xs outline-none"
                            />
                            <div className="w-52">
                              <NotebookSelect
                                value={newLessonType}
                                onChange={(val) => setNewLessonType(val)}
                                options={[
                                  { value: 'Vocabulary', label: language === 'vi' ? 'Từ vựng (Vocabulary)' : 'Vocabulary' },
                                  { value: 'Grammar', label: language === 'vi' ? 'Ngữ pháp (Grammar)' : 'Grammar' },
                                  { value: 'Listening', label: language === 'vi' ? 'Luyện nghe (Listening)' : 'Listening' },
                                  { value: 'Reading', label: language === 'vi' ? 'Đọc hiểu (Reading)' : 'Reading' },
                                  { value: 'Writing', label: language === 'vi' ? 'Luyện viết (Writing)' : 'Writing' }
                                ]}
                                className="w-full text-xs"
                              />
                            </div>
                            <button
                              type="button"
                              onClick={() => handleCreateLesson(unit.id)}
                              className="px-3 py-1.5 bg-stone-900 text-white rounded text-xs font-bold notebook-btn cursor-pointer"
                            >
                              Lưu bài học
                            </button>
                            <button
                              type="button"
                              onClick={() => setActiveUnitForLesson(null)}
                              className="px-2 py-1.5 bg-white hover:bg-stone-100 text-stone-600 rounded text-xs border border-stone-300 cursor-pointer"
                            >
                              Đóng
                            </button>
                          </div>
                        )}

                        {/* Danh sách bài học */}
                        <div className="space-y-1">
                          {unit.lessons?.length === 0 ? (
                            <span className="text-[11px] text-stone-400 italic block py-1">
                              Chưa có bài học nào trong Unit này.
                            </span>
                          ) : (
                            unit.lessons.map((lesson) => (
                              <div
                                key={lesson.id}
                                className="flex items-center justify-between p-2 rounded bg-stone-50 hover:bg-stone-100 transition"
                              >
                                <div className="flex items-center gap-2 truncate">
                                  <BookOutlined className="text-stone-400 text-xs shrink-0" />
                                  <span className="font-semibold text-xs text-stone-800 truncate">
                                    {lesson.title}
                                  </span>
                                  {lesson.lessonType && (
                                    <span className="text-[10px] font-mono px-1 rounded bg-stone-200 text-stone-700">
                                      {lesson.lessonType}
                                    </span>
                                  )}
                                </div>
                                <div className="flex items-center gap-2">
                                  <button type="button" onClick={() => setEditingLessonId(lesson.id)} className="rounded border border-amber-300 bg-amber-50 px-2 py-1 text-[11px] font-semibold text-amber-900">
                                    {language === 'vi' ? 'Sửa nội dung' : 'Edit content'}
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => handleDeleteLesson(lesson.id)}
                                    className="p-1 text-stone-400 hover:text-rose-600 transition cursor-pointer"
                                    title="Xóa bài học"
                                  >
                                    <DeleteOutlined className="text-xs" />
                                  </button>
                                </div>
                              </div>
                            ))
                          )}
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            ) : null}

            <div className="flex justify-end pt-2 border-t border-stone-200">
              <button
                type="button"
                onClick={() => setIsManageModalOpen(false)}
                className="px-4 py-1.5 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn cursor-pointer"
              >
                Hoàn tất
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: NHẬP BÀI HỌC JSON VÀO KHÓA HỌC HIỆN TẠI */}
      {isCourseImportModalOpen && managedCourse && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs overflow-y-auto">
          <div className="bg-[#FAF6EE] border-2 border-[#262626] rounded-2xl shadow-notebook max-w-2xl w-full p-6 space-y-4 my-8">
            <div className="flex items-center justify-between border-b-2 border-[#262626] pb-3">
              <div className="flex items-center gap-2">
                <UploadOutlined className="text-amber-700 text-lg" />
                <div>
                  <h3 className="text-sm font-bold text-stone-900">
                    Nhập Bài học JSON vào Khóa: {managedCourse.title}
                  </h3>
                  <div className="text-[11px] text-stone-500 font-mono">
                    {managedCourse.level} • {(managedCourse.domain ?? 'general').toUpperCase()}
                  </div>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setIsCourseImportModalOpen(false)}
                className="p-1 text-stone-500 hover:text-stone-900 cursor-pointer"
              >
                <CloseOutlined />
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <p className="text-stone-600">
                Tải lên file JSON chứa các Units & Lessons (gồm Flashcard từ vựng, Ngữ pháp, Bài nghe, Bài nói, Đọc hiểu, Bài viết và Quiz) được thiết kế riêng cho khóa học này.
              </p>

              <div className="flex items-center gap-3">
                <input
                  type="file"
                  ref={singleCourseFileInputRef}
                  accept=".json"
                  onChange={handleSingleCourseFileUpload}
                  className="hidden"
                />
                <button
                  type="button"
                  onClick={() => singleCourseFileInputRef.current?.click()}
                  className="px-3 py-1.5 bg-white border-notebook rounded-lg text-xs font-bold hover:bg-stone-100 flex items-center gap-1.5 cursor-pointer"
                >
                  <UploadOutlined />
                  <span>Chọn file JSON từ máy tính</span>
                </button>
              </div>

              <div>
                <label className="block font-bold text-stone-800 mb-1">Nội dung JSON bài học:</label>
                <textarea
                  rows={8}
                  value={courseImportJsonText}
                  onChange={(e) => setCourseImportJsonText(e.target.value)}
                  placeholder="Dán nội dung JSON của khóa học vào đây..."
                  className="w-full p-2.5 font-mono text-xs bg-white border border-stone-300 rounded-lg outline-none"
                />
              </div>

              <p className="text-stone-600">Điền đầy đủ các trường trong JSON mẫu trước khi nhập. Khóa học cần chưa có Unit.</p>
              {courseImportError && <p role="alert" className="rounded-lg border border-rose-300 bg-rose-50 p-2 text-rose-800">{courseImportError}</p>}

              <div className="flex justify-end gap-2 pt-2 border-t border-stone-200">
                <button
                  type="button"
                  onClick={() => setIsCourseImportModalOpen(false)}
                  className="px-3.5 py-1.5 rounded-lg border border-stone-300 bg-white hover:bg-stone-100 text-xs font-bold cursor-pointer"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="button"
                  disabled={!courseImportJsonText.trim()}
                  onClick={() => handleImportCourseLessons(courseImportJsonText)}
                  className="px-4 py-1.5 rounded-lg bg-stone-900 text-white text-xs font-bold notebook-btn flex items-center gap-1.5 disabled:opacity-50 cursor-pointer"
                >
                  <CheckCircleOutlined />
                  <span>Xác nhận Nhập Bài học</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {isPlacementEditorOpen && managedCourse && languages.find((item) => item.id === managedCourse.languageId) && (
        <PlacementEditor
          course={managedCourse}
          language={languages.find((item) => item.id === managedCourse.languageId)!}
          onClose={() => setIsPlacementEditorOpen(false)}
        />
      )}
      {isSpecialPracticeEditorOpen && managedCourse && <SpecialPracticeEditor course={managedCourse} initialSection={specialPracticeSection} onClose={() => setIsSpecialPracticeEditorOpen(false)} />}
      {isCategoryManagerOpen && <CourseCategoryManager language={language} onClose={() => setIsCategoryManagerOpen(false)} onChanged={() => setCategoryRevision(value => value + 1)} />}
      {editingLessonId && managedCourse && (
        <LessonMaterialEditor
          courseId={managedCourse.id}
          lessonId={editingLessonId}
          onClose={() => setEditingLessonId(null)}
          onSaved={() => { void reloadManagedCourse(); }}
        />
      )}
      {isPlacementReviewOpen && <PlacementReviewPanel onClose={() => setIsPlacementReviewOpen(false)} />}
      {isSpecialPracticeReviewOpen && <SpecialPracticeReviewPanel onClose={() => setIsSpecialPracticeReviewOpen(false)} />}
    </div>
  );
};

