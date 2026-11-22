using LanguageLearning.Application.Interfaces;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public sealed class DataSeederTests
{
    [Fact]
    public async Task FreshDatabase_RequiresConfiguredAdminPassword()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DataSeeder.SeedAsync(context, new PasswordHasher(), null));
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task FreshDatabase_SeedsOnlyAdminAndDoesNotCreateCurriculum()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        IPasswordHasher hasher = new PasswordHasher();
        await DataSeeder.SeedAsync(context, hasher, "Admin@123456");
        await DataSeeder.SeedAsync(context, hasher, null);

        var admin = Assert.Single(await context.Users.ToListAsync());
        Assert.Equal("admin@languagelearning.app", admin.Email);
        Assert.Empty(await context.Languages.ToListAsync());
        Assert.Empty(await context.Courses.ToListAsync());
        Assert.Empty(await context.Decks.ToListAsync());
    }
}
