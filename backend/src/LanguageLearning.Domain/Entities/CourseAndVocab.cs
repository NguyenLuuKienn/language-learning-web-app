using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Enums;

namespace LanguageLearning.Domain.Entities;

public class Language : BaseEntity
{
    public string Code { get; set; } = string.Empty; // en, ja, ko...
    public string Name { get; set; } = string.Empty; // English, Japanese...
    public bool IsActive { get; set; } = true;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Deck> Decks { get; set; } = new List<Deck>();
}

public class Course : BaseEntity
{
    public Guid LanguageId { get; set; }
    public Language Language { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = "A1";
    public string Description { get; set; } = string.Empty;
    public ContentStatus Status { get; set; } = ContentStatus.Published;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}

public class Unit : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson : BaseEntity
{
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Published;

    public ICollection<Vocabulary> Vocabularies { get; set; } = new List<Vocabulary>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}

public class Vocabulary : BaseEntity
{
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public Guid LanguageId { get; set; }
    public Language Language { get; set; } = null!;
    public string Term { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
}
