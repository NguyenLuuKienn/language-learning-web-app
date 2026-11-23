using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Data;

public static class A1SampleContentSeeder
{
    public static readonly Guid CourseId = Guid.Parse("a1000000-0000-4000-8000-000000000019");
    public static readonly Guid LessonId = Guid.Parse("a1000000-0000-4000-8000-000000000020");

    private static readonly (string Term, string Phonetic, string Meaning, string Example)[] Words =
    [
        ("hello", "həˈloʊ", "xin chào", "Hello, my name is Anna."),
        ("goodbye", "ɡʊdˈbaɪ", "tạm biệt", "Goodbye, see you tomorrow."),
        ("please", "pliːz", "làm ơn", "Please open the door."),
        ("thank you", "ˈθæŋk juː", "cảm ơn", "Thank you for your help."),
        ("yes", "jes", "vâng", "Yes, I am a student."),
        ("no", "noʊ", "không", "No, I am not tired."),
        ("friend", "frend", "bạn bè", "She is my friend."),
        ("family", "ˈfæməli", "gia đình", "My family is at home."),
        ("school", "skuːl", "trường học", "I go to school every day."),
        ("book", "bʊk", "quyển sách", "This book is new."),
    ];

    public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)
    {
        var language = await context.Languages.FirstOrDefaultAsync(value => value.Code == "en", ct);
        if (language is null)
        {
            language = new Language { Code = "en", Name = "English" };
            context.Languages.Add(language);
        }
        else
        {
            language.IsActive = true;
        }

        if (!await context.Courses.AnyAsync(value => value.Id == CourseId, ct))
        {
            var course = new Course
            {
                Id = CourseId,
                Language = language,
                Title = "English A1: Everyday Words",
                Level = "A1",
                Description = "Ten essential English words for beginners.",
                Status = ContentStatus.Published,
            };
            var unit = new Unit { Course = course, Title = "Everyday English", SortOrder = 1 };
            context.Lessons.Add(new Lesson
            {
                Id = LessonId,
                Unit = unit,
                Title = "First words",
                Content = "Listen to each word, read the example, and practice saying it aloud.",
                SortOrder = 1,
                Status = ContentStatus.Published,
            });
        }

        var existing = await context.Vocabularies.Where(value => value.LessonId == LessonId)
            .Select(value => value.Term).ToListAsync(ct);
        foreach (var word in Words.Where(value => !existing.Contains(value.Term)))
        {
            context.Vocabularies.Add(new Vocabulary
            {
                LessonId = LessonId,
                Language = language,
                Term = word.Term,
                Phonetic = word.Phonetic,
                Meaning = word.Meaning,
                Example = word.Example,
            });
        }
        await context.SaveChangesAsync(ct);
    }
}
