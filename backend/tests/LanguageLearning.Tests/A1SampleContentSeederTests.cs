using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Tests;

public class A1SampleContentSeederTests
{
    [Fact]
    public async Task SeedsPublishedA1LessonWithTenWordsAndCanRunAgain()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await A1SampleContentSeeder.SeedAsync(context);
        await A1SampleContentSeeder.SeedAsync(context);

        var course = await context.Courses.SingleAsync(value => value.Id == A1SampleContentSeeder.CourseId);
        Assert.Equal("A1", course.Level);
        Assert.Equal(ContentStatus.Published, course.Status);
        Assert.Equal(ContentStatus.Published, (await context.Lessons.SingleAsync(value => value.Id == A1SampleContentSeeder.LessonId)).Status);
        var words = await context.Vocabularies.Where(value => value.LessonId == A1SampleContentSeeder.LessonId).ToListAsync();
        Assert.Equal(10, words.Count);
        Assert.All(words, word =>
        {
            Assert.False(string.IsNullOrWhiteSpace(word.Term));
            Assert.False(string.IsNullOrWhiteSpace(word.Phonetic));
            Assert.False(string.IsNullOrWhiteSpace(word.Meaning));
            Assert.False(string.IsNullOrWhiteSpace(word.Example));
        });
        var viewer = await new PublishedContentRepository(context).GetLessonViewerAsync(A1SampleContentSeeder.LessonId, default);
        Assert.NotNull(viewer);
        Assert.Equal("en", viewer.LanguageCode);
        Assert.Equal(10, viewer.Vocabulary.Count);
    }
}
