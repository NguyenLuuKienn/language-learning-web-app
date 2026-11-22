using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContext<AppDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            var passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();

            db.Database.EnsureCreated();
            DataSeeder.SeedAsync(db, passwordHasher, "Admin@123456").GetAwaiter().GetResult();
            SeedTestDataAsync(db, passwordHasher).GetAwaiter().GetResult();
        });

        builder.UseEnvironment("Development");
    }

    private static async Task SeedTestDataAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        var englishId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (!await db.Languages.AnyAsync())
        {
            db.Languages.AddRange(
                new Language { Id = englishId, Code = "en", Name = "English", IsActive = true },
                new Language { Id = Guid.NewGuid(), Code = "ja", Name = "Japanese", IsActive = true });
        }

        if (!await db.Users.AnyAsync(user => user.Email == "learner@languagelearning.app"))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "learner@languagelearning.app",
                PasswordHash = passwordHasher.HashPassword("Learner@123456"),
                DisplayName = "Alex Johnson",
                Role = UserRole.Learner,
                IsActive = true
            });
        }

        if (!await db.Courses.AnyAsync())
        {
            var courseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            db.Courses.Add(new Course
            {
                Id = courseId, LanguageId = englishId, Title = "English A1 test course",
                Level = "A1", Domain = "general", Status = ContentStatus.Published,
                Units = [new Unit
                {
                    Id = Guid.NewGuid(), Title = "Test unit", SortOrder = 1,
                    Lessons = [new Lesson { Id = Guid.NewGuid(), Title = "Test lesson", SortOrder = 1, Status = ContentStatus.Published }]
                }]
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "learner@languagelearning.app",
        string password = "Learner@123456")
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        response.EnsureSuccessStatusCode();

        var accessCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("ll_access="));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", accessCookie.Split(';', 2)[0].Split('=', 2)[1]);

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
