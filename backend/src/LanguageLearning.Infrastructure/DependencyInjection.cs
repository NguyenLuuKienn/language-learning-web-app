
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=language_learning.db";

        var provider = configuration["DatabaseProvider"];
        var isSqlite = string.IsNullOrWhiteSpace(provider)
            ? connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase)
            : string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<AppDbContext>(options =>
        {
            if (isSqlite)
            {
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
            }
            else
            {
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
                options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                {
                    mySqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            }
        });

        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<IStudyAccessService, Security.StudyAccessService>();
        services.AddScoped<IPasswordHasher, Security.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Security.JwtTokenGenerator>();
        services.AddSingleton<IPlacementProofService, Security.PlacementProofService>();
        services.AddScoped<IUserRepository, Repositories.UserRepository>();
        services.AddScoped<IWritingSubmissionRepository, WritingSubmissionRepository>();
        services.AddScoped<ICourseRepository, Repositories.CourseRepository>();
        services.AddScoped<ICourseCategoryRepository, Repositories.CourseCategoryRepository>();
        services.AddScoped<IVocabularyRepository, Repositories.VocabularyRepository>();
        services.AddScoped<ICardRepository, Repositories.CardRepository>();
        services.AddScoped<IReviewRepository, Repositories.ReviewRepository>();
        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<IVocabularySessionRepository, VocabularySessionRepository>();
        services.AddScoped<ISkillPracticeRepository, SkillPracticeRepository>();
        services.AddScoped<ISpecialPracticeRepository, SpecialPracticeRepository>();
        services.AddScoped<IPlacementReviewRepository, PlacementReviewRepository>();
        services.AddScoped<IQuizRepository, Repositories.QuizRepository>();
        services.AddScoped<IProgressRepository, Repositories.ProgressRepository>();
        services.AddScoped<ILessonJourneyRepository, Repositories.LessonJourneyRepository>();
        services.AddScoped<IMediaService, Services.MediaService>();
        services.AddScoped<ISyncRepository, Repositories.SyncRepository>();
        services.AddScoped<IAdminRepository, Repositories.AdminRepository>();

        return services;
    }
}
