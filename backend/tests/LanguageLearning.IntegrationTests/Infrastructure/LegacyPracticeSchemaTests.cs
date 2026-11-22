using LanguageLearning.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public sealed class LegacyPracticeSchemaTests
{
    [Fact]
    public async Task ExistingDatabase_GainsPracticeTablesWithoutLosingOtherData()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE skill_practice_progresses");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE special_practice_attempts");

        await LegacySchemaUpgrade.EnsurePracticeTablesAsync(db);
        await LegacySchemaUpgrade.EnsurePracticeTablesAsync(db);

        Assert.Equal(0, await db.SkillPracticeProgresses.CountAsync());
        Assert.Equal(0, await db.SpecialPracticeAttempts.CountAsync());
    }
}
