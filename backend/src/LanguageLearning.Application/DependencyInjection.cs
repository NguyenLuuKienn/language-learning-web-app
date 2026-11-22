using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudyTargetService, StudyTargetService>();
        services.AddScoped<IPlacementService, PlacementService>();
        services.AddScoped<IWritingSubmissionService, WritingSubmissionService>();
        services.AddScoped<IDeckService, DeckService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<ICourseMaterialService, CourseMaterialService>();
        services.AddScoped<IVocabularyService, VocabularyService>();
        services.AddSingleton<ISrsScheduler, SrsScheduler>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<ISrsService, SrsService>();
        services.AddScoped<INotebookService, NotebookService>();
        services.AddScoped<IVocabularySessionService, VocabularySessionService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<ILessonJourneyService, LessonJourneyService>();
        services.AddScoped<ISkillPracticeService, SkillPracticeService>();
        services.AddScoped<ISpecialPracticeService, SpecialPracticeService>();
        services.AddScoped<ICsvService, CsvService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<ISharingService, SharingService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
