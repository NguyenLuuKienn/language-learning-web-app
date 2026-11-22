using LanguageLearning.Application.Common;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Application.Validation;

namespace LanguageLearning.Application.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseCategoryRepository? _categories;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public CourseService(ICourseRepository courseRepository, ICourseCategoryRepository categories) : this(courseRepository)
    {
        _categories = categories;
    }



    public async Task<List<CourseResponse>> GetCoursesAsync(Guid? languageId, string? level, string? domain = null, Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        return await _courseRepository.GetCourseSummariesAsync(languageId, level, domain, currentUserId, cancellationToken);
    }

    public async Task<CourseDetailResponse> GetCourseDetailAsync(Guid courseId, Guid? currentUserId = null, CancellationToken cancellationToken = default, StudyTargetDto? target = null, bool isAdmin = false)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, includeUnitsAndLessons: true, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException(Messages.Course.CourseNotFound);
        }
        if (currentUserId.HasValue && !isAdmin && !CourseAccessPolicy.CanAccess(course, target))
            throw new NotFoundException(Messages.Course.CourseNotFound);

        bool isEnrolled = false;
        var userProgresses = new List<LessonProgress>();
        if (currentUserId.HasValue)
        {
            isEnrolled = await _courseRepository.IsEnrolledAsync(currentUserId.Value, courseId, cancellationToken);
            userProgresses = await _courseRepository.GetUserLessonProgressesAsync(currentUserId.Value, courseId, cancellationToken);
        }

        var progressMap = userProgresses.ToDictionary(p => p.LessonId, p => p.Status);

        return new CourseDetailResponse
        {
            Id = course.Id,
            LanguageId = course.LanguageId,
            LanguageName = course.Language?.Name ?? string.Empty,
            LanguageCode = course.Language?.Code ?? string.Empty,
            Title = course.Title,
            Level = course.Level,
            Domain = course.Domain,
            Description = course.Description,
            HasMaterial = !string.IsNullOrWhiteSpace(course.MaterialJson) && course.Units.Count > 0,
            Status = course.Status,
            UnitsCount = course.Units.Count,
            LessonsCount = course.Units.SelectMany(u => u.Lessons).Count(),
            IsEnrolled = isEnrolled,
            Units = course.Units
                .OrderBy(u => u.SortOrder > 0 ? u.SortOrder : int.MaxValue)
                .ThenBy(u => u.Title)
                .Select(u => new UnitResponse
            {
                Id = u.Id,
                CourseId = u.CourseId,
                Title = u.Title,
                SortOrder = u.SortOrder,
                Lessons = u.Lessons
                    .OrderBy(l => l.SortOrder > 0 ? l.SortOrder : int.MaxValue)
                    .ThenBy(l => l.Title)
                    .Select(l => new LessonSummaryResponse
                {
                    Id = l.Id,
                    UnitId = l.UnitId,
                    Title = l.Title,
                    LessonType = l.LessonType,
                    SortOrder = l.SortOrder,
                    Status = l.Status,
                    VocabCount = l.Vocabularies.Count,
                    ProgressStatus = progressMap.TryGetValue(l.Id, out var status) ? status : "NotStarted"
                }).ToList()
            }).ToList()
        };
    }

    public async Task EnrollCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default, StudyTargetDto? target = null, bool isAdmin = false)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, false, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException(Messages.Course.CourseNotFound);
        }
        if (!isAdmin && !CourseAccessPolicy.CanAccess(course, target))
            throw new NotFoundException(Messages.Course.CourseNotFound);

        await _courseRepository.EnrollAsync(userId, courseId, cancellationToken);
    }

    public async Task<CurriculumExportResponse> ExportCurriculumAsync(Guid? languageId = null, CancellationToken cancellationToken = default)
    {
        var languages = await _courseRepository.GetLanguagesAsync(cancellationToken);
        var courses = await _courseRepository.GetAllCoursesForExportAsync(languageId, cancellationToken);

        var langResponses = languages.Select(l => new LanguageResponse
        {
            Id = l.Id,
            Code = l.Code,
            Name = l.Name,
            NativeName = l.NativeName,
            FlagEmoji = l.FlagEmoji,
            IsActive = l.IsActive
        }).ToList();

        var courseDtos = courses.Select(c => new CurriculumCourseDto
        {
            LanguageCode = c.Language?.Code ?? string.Empty,
            Title = c.Title,
            Level = c.Level,
            Domain = c.Domain,
            Description = c.Description,
            Units = c.Units.OrderBy(u => u.SortOrder).Select(u => new CurriculumUnitDto
            {
                Title = u.Title,
                SortOrder = u.SortOrder,
                Lessons = u.Lessons.OrderBy(l => l.SortOrder).Select(l => new CurriculumLessonDto
                {
                    Title = l.Title,
                    Content = l.Content,
                    LessonType = l.LessonType,
                    SortOrder = l.SortOrder,
                    Vocabularies = l.Vocabularies.Select(v => new CurriculumVocabDto
                    {
                        Term = v.Term,
                        Phonetic = v.Phonetic,
                        Meaning = v.Meaning,
                        Example = v.Example,
                        AudioUrl = v.AudioUrl
                    }).ToList()
                }).ToList()
            }).ToList()
        }).ToList();

        return new CurriculumExportResponse
        {
            ExportedAt = DateTime.UtcNow,
            Languages = langResponses,
            Courses = courseDtos
        };
    }

    public async Task<LessonDetailResponse> GetLessonDetailAsync(Guid lessonId, Guid? currentUserId = null, CancellationToken cancellationToken = default, StudyTargetDto? target = null, bool isAdmin = false)
    {
        var lesson = await _courseRepository.GetLessonByIdAsync(lessonId, includeVocabularies: true, cancellationToken);
        if (lesson == null)
        {
            throw new NotFoundException(Messages.Course.LessonNotFound);
        }
        if (currentUserId.HasValue && !isAdmin && !CourseAccessPolicy.CanAccess(lesson.Unit.Course, target))
            throw new NotFoundException(Messages.Course.LessonNotFound);

        string progressStatus = "NotStarted";
        if (currentUserId.HasValue)
        {
            var unit = lesson.Unit;
            if (unit != null)
            {
                var progresses = await _courseRepository.GetUserLessonProgressesAsync(currentUserId.Value, unit.CourseId, cancellationToken);
                var current = progresses.FirstOrDefault(p => p.LessonId == lessonId);
                if (current != null)
                {
                    progressStatus = current.Status;
                }
            }
        }

        return new LessonDetailResponse
        {
            Id = lesson.Id,
            UnitId = lesson.UnitId,
            Title = lesson.Title,
            Content = lesson.Content,
            LessonType = lesson.LessonType,
            SortOrder = lesson.SortOrder,
            Status = lesson.Status,
            VocabCount = lesson.Vocabularies.Count,
            ProgressStatus = progressStatus,
            Vocabularies = lesson.Vocabularies.Select(v => new VocabularyResponse
            {
                Id = v.Id,
                LessonId = v.LessonId,
                LanguageId = v.LanguageId,
                Term = v.Term,
                Phonetic = v.Phonetic,
                Meaning = v.Meaning,
                Meanings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(v.MeaningsJson) is { Count: > 0 } meanings ? meanings : [v.Meaning],
                PartOfSpeech = v.PartOfSpeech,
                Example = v.Example,
                AudioUrl = v.AudioUrl,
                ImageUrl = v.ImageUrl
            }).ToList()
        };
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var title = NormalizeCourseTitle(request.Title);
        var description = NormalizeCourseDescription(request.Description);
        var level = NormalizeCourseCategory(request.Level, 20, "Level");
        var domain = NormalizeCourseCategory(request.Domain, 50, "Domain").ToLowerInvariant();
        var language = await _courseRepository.GetLanguageByIdAsync(request.LanguageId, cancellationToken);
        if (language == null)
        {
            throw new NotFoundException(Messages.Course.LanguageNotFound);
        }
        await ValidateCategoriesAsync(language.Code, level, domain, cancellationToken);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            LanguageId = request.LanguageId,
            Title = title,
            Level = level,
            Domain = domain,
            Description = description,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddCourseAsync(course, cancellationToken);

        return new CourseResponse
        {
            Id = course.Id,
            LanguageId = course.LanguageId,
            LanguageName = language.Name,
            LanguageCode = language.Code,
            Title = course.Title,
            Level = course.Level,
            Domain = course.Domain,
            Description = course.Description,
            Status = course.Status,
            UnitsCount = 0,
            LessonsCount = 0,
            IsEnrolled = false
        };
    }

    public async Task<CourseResponse> UpdateCourseAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var title = NormalizeCourseTitle(request.Title);
        var description = NormalizeCourseDescription(request.Description);
        var level = NormalizeCourseCategory(request.Level, 20, "Level");
        var domain = NormalizeCourseCategory(request.Domain, 50, "Domain").ToLowerInvariant();
        var course = await _courseRepository.GetCourseByIdAsync(id, false, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException(Messages.Course.CourseNotFound);
        }
        await ValidateCategoriesAsync(course.Language.Code, level, domain, cancellationToken);

        course.Title = title;
        course.Level = level;
        course.Domain = domain;
        course.Description = description;
        course.Status = request.Status;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateCourseAsync(course, cancellationToken);

        var language = await _courseRepository.GetLanguageByIdAsync(course.LanguageId, cancellationToken);

        return new CourseResponse
        {
            Id = course.Id,
            LanguageId = course.LanguageId,
            LanguageName = language?.Name ?? string.Empty,
            LanguageCode = language?.Code ?? string.Empty,
            Title = course.Title,
            Level = course.Level,
            Domain = course.Domain,
            Description = course.Description,
            Status = course.Status,
            UnitsCount = 0,
            LessonsCount = 0,
            IsEnrolled = false
        };
    }

    private async Task ValidateCategoriesAsync(string languageCode, string level, string domain, CancellationToken cancellationToken)
    {
        if (_categories is null)
        {
            CourseClassification.Validate(languageCode, level, domain);
            return;
        }
        var code = languageCode.Trim().ToLowerInvariant();
        var framework = code switch
        {
            "ja" or "jp" => "jlpt",
            "zh" or "cn" => "hsk",
            "ko" or "kr" => "topik",
            _ when code.Contains("japan") => "jlpt",
            _ when code.Contains("chin") => "hsk",
            _ when code.Contains("korea") => "topik",
            _ => "cefr"
        };
        if (!await _categories.ExistsAsync("level", framework, level, cancellationToken))
            throw new ArgumentException("Course level does not match the language framework.");
        if (!await _categories.ExistsAsync("domain", "", domain, cancellationToken))
            throw new ArgumentException("Course domain is not supported.");
    }

    private static string NormalizeCourseTitle(string? title)
    {
        var value = title?.Trim() ?? string.Empty;
        if (value.Length < 2 || value.Length > 200 || value.Any(char.IsControl))
            throw new BusinessRuleException(ErrorCodes.CourseTitleInvalid, Messages.Course.TitleInvalid);
        return value;
    }

    private static string NormalizeCourseDescription(string? description)
    {
        var value = description?.Trim() ?? string.Empty;
        if (value.Length > 2000 || value.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new ArgumentException(Messages.Course.DescriptionInvalid);
        return value;
    }

    private static string NormalizeCourseCategory(string? category, int maxLength, string name)
    {
        var value = category?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.Length > maxLength || value.Any(char.IsControl))
            throw new ArgumentException(Messages.Course.RequiredWithMaxLength(name, maxLength));
        return value;
    }

    public async Task DeleteCourseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(id, true, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException(Messages.Course.CourseNotFound);
        }

        await _courseRepository.DeleteCourseAsync(course, cancellationToken);
    }

    public async Task<UnitResponse> CreateUnitAsync(Guid courseId, CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseByIdAsync(courseId, false, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException(Messages.Course.CourseNotFound);
        }

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title.Trim(),
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddUnitAsync(unit, cancellationToken);

        return new UnitResponse
        {
            Id = unit.Id,
            CourseId = unit.CourseId,
            Title = unit.Title,
            SortOrder = unit.SortOrder,
            Lessons = new List<LessonSummaryResponse>()
        };
    }

    public async Task<UnitResponse> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await _courseRepository.GetUnitByIdAsync(id, cancellationToken);
        if (unit == null)
        {
            throw new NotFoundException(Messages.Course.UnitNotFound);
        }

        unit.Title = request.Title.Trim();
        unit.SortOrder = request.SortOrder;
        unit.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateUnitAsync(unit, cancellationToken);

        return new UnitResponse
        {
            Id = unit.Id,
            CourseId = unit.CourseId,
            Title = unit.Title,
            SortOrder = unit.SortOrder
        };
    }

    public async Task DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _courseRepository.GetUnitByIdAsync(id, cancellationToken);
        if (unit == null)
        {
            throw new NotFoundException(Messages.Course.UnitNotFound);
        }

        await _courseRepository.DeleteUnitAsync(unit, cancellationToken);
    }

    public async Task<LessonDetailResponse> CreateLessonAsync(Guid unitId, CreateLessonRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await _courseRepository.GetUnitByIdAsync(unitId, cancellationToken);
        if (unit == null)
        {
            throw new NotFoundException(Messages.Course.UnitNotFound);
        }

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            Title = request.Title.Trim(),
            Content = request.Content,
            LessonType = request.LessonType,
            SortOrder = request.SortOrder,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddLessonAsync(lesson, cancellationToken);

        return new LessonDetailResponse
        {
            Id = lesson.Id,
            UnitId = lesson.UnitId,
            Title = lesson.Title,
            Content = lesson.Content,
            LessonType = lesson.LessonType,
            SortOrder = lesson.SortOrder,
            Status = lesson.Status,
            VocabCount = 0,
            ProgressStatus = "NotStarted",
            Vocabularies = new List<VocabularyResponse>()
        };
    }

    public async Task<LessonDetailResponse> UpdateLessonAsync(Guid id, UpdateLessonRequest request, CancellationToken cancellationToken = default)
    {
        var lesson = await _courseRepository.GetLessonByIdAsync(id, true, cancellationToken);
        if (lesson == null)
        {
            throw new NotFoundException(Messages.Course.LessonNotFound);
        }

        lesson.Title = request.Title.Trim();
        lesson.Content = request.Content;
        lesson.LessonType = request.LessonType;
        lesson.SortOrder = request.SortOrder;
        lesson.Status = request.Status;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateLessonAsync(lesson, cancellationToken);

        return new LessonDetailResponse
        {
            Id = lesson.Id,
            UnitId = lesson.UnitId,
            Title = lesson.Title,
            Content = lesson.Content,
            LessonType = lesson.LessonType,
            SortOrder = lesson.SortOrder,
            Status = lesson.Status,
            VocabCount = lesson.Vocabularies.Count,
            ProgressStatus = "NotStarted",
            Vocabularies = lesson.Vocabularies.Select(v => new VocabularyResponse
            {
                Id = v.Id,
                LessonId = v.LessonId,
                LanguageId = v.LanguageId,
                Term = v.Term,
                Phonetic = v.Phonetic,
                Meaning = v.Meaning,
                Meanings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(v.MeaningsJson) is { Count: > 0 } meanings ? meanings : [v.Meaning],
                PartOfSpeech = v.PartOfSpeech,
                Example = v.Example,
                AudioUrl = v.AudioUrl,
                ImageUrl = v.ImageUrl
            }).ToList()
        };
    }

    public async Task DeleteLessonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lesson = await _courseRepository.GetLessonByIdAsync(id, false, cancellationToken);
        if (lesson == null)
        {
            throw new NotFoundException(Messages.Course.LessonNotFound);
        }

        await _courseRepository.DeleteLessonAsync(lesson, cancellationToken);
    }
}
