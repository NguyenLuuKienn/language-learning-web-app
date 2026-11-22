using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<NotebookEntry> NotebookEntries => Set<NotebookEntry>();
    public DbSet<VocabularySession> VocabularySessions => Set<VocabularySession>();
    public DbSet<SkillPracticeProgress> SkillPracticeProgresses => Set<SkillPracticeProgress>();
    public DbSet<SpecialPracticeAttempt> SpecialPracticeAttempts => Set<SpecialPracticeAttempt>();
    public DbSet<WritingSubmission> WritingSubmissions => Set<WritingSubmission>();
    public DbSet<PlacementAttempt> PlacementAttempts => Set<PlacementAttempt>();
    public DbSet<PlacementAudio> PlacementAudios => Set<PlacementAudio>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Vocabulary> Vocabularies => Set<Vocabulary>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckTag> DeckTags => Set<DeckTag>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardSchedule> CardSchedules => Set<CardSchedule>();
    public DbSet<CardLessonSource> CardLessonSources => Set<CardLessonSource>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<LessonStepProgress> LessonStepProgresses => Set<LessonStepProgress>();
    public DbSet<SkillPracticeEvent> SkillPracticeEvents => Set<SkillPracticeEvent>();
    public DbSet<LearningSession> LearningSessions => Set<LearningSession>();
    public DbSet<LearningEvidence> LearningEvidences => Set<LearningEvidence>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<DailyLearningStats> DailyLearningStats => Set<DailyLearningStats>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<NotebookEntry>().ToTable("notebook_entries");
        modelBuilder.Entity<NotebookEntry>().Property(entry => entry.Text).HasMaxLength(500);
        modelBuilder.Entity<NotebookEntry>().Property(entry => entry.Note).HasMaxLength(5000);
        modelBuilder.Entity<NotebookEntry>().Property(entry => entry.LanguageCode).HasMaxLength(12);
        modelBuilder.Entity<NotebookEntry>().Property(entry => entry.SourcePath).HasMaxLength(500);
        modelBuilder.Entity<NotebookEntry>().Property(entry => entry.Context).HasMaxLength(1000);
        modelBuilder.Entity<NotebookEntry>().HasIndex(entry => new { entry.UserId, entry.CreatedAt });
        modelBuilder.Entity<VocabularySession>().ToTable("vocabulary_sessions");
        modelBuilder.Entity<VocabularySession>().Property(session => session.StepsJson).HasColumnType("longtext");
        modelBuilder.Entity<VocabularySession>().Property(session => session.ResultsJson).HasColumnType("longtext");
        modelBuilder.Entity<VocabularySession>().HasIndex(session => new { session.UserId, session.LessonId, session.CompletedAt });
        modelBuilder.Entity<SkillPracticeProgress>().ToTable("skill_practice_progresses");
        modelBuilder.Entity<SkillPracticeEvent>().ToTable("skill_practice_events");
        modelBuilder.Entity<SkillPracticeEvent>().Property(item => item.Kind).HasMaxLength(20);
        modelBuilder.Entity<SkillPracticeEvent>().Property(item => item.ActivityId).HasMaxLength(100);
        modelBuilder.Entity<SkillPracticeEvent>().HasIndex(item => new { item.UserId, item.ClientAttemptId }).IsUnique();
        modelBuilder.Entity<SkillPracticeProgress>().Property(item => item.Kind).HasMaxLength(20);
        modelBuilder.Entity<SkillPracticeProgress>().Property(item => item.ActivityId).HasMaxLength(100);
        modelBuilder.Entity<SkillPracticeProgress>().Property(item => item.Status).HasMaxLength(20);
        modelBuilder.Entity<SkillPracticeProgress>().HasIndex(item => new { item.UserId, item.CourseId, item.Kind, item.ActivityId }).IsUnique();
        modelBuilder.Entity<SpecialPracticeAttempt>().ToTable("special_practice_attempts");
        modelBuilder.Entity<SpecialPracticeAttempt>().Property(item => item.Kind).HasMaxLength(20);
        modelBuilder.Entity<SpecialPracticeAttempt>().Property(item => item.Status).HasMaxLength(20);
        modelBuilder.Entity<SpecialPracticeAttempt>().Property(item => item.ContentSnapshotJson).HasColumnType("longtext");
        modelBuilder.Entity<SpecialPracticeAttempt>().Property(item => item.AnswersJson).HasColumnType("longtext");
        modelBuilder.Entity<SpecialPracticeAttempt>().Property(item => item.EvaluationJson).HasColumnType("longtext");
        modelBuilder.Entity<SpecialPracticeAttempt>().HasIndex(item => new { item.UserId, item.CourseId, item.ContentId, item.Status });
        modelBuilder.Entity<WritingSubmission>().ToTable("writing_submissions");
        modelBuilder.Entity<PlacementAttempt>().ToTable("placement_attempts");
        modelBuilder.Entity<PlacementAudio>().ToTable("placement_audios");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
        modelBuilder.Entity<Language>().ToTable("languages");
        modelBuilder.Entity<CourseCategory>().ToTable("course_categories");
        modelBuilder.Entity<CourseCategory>().Property(item => item.Kind).HasMaxLength(20);
        modelBuilder.Entity<CourseCategory>().Property(item => item.Framework).HasMaxLength(20);
        modelBuilder.Entity<CourseCategory>().Property(item => item.Code).HasMaxLength(50);
        modelBuilder.Entity<CourseCategory>().Property(item => item.TitleVi).HasMaxLength(150);
        modelBuilder.Entity<CourseCategory>().Property(item => item.TitleEn).HasMaxLength(150);
        modelBuilder.Entity<CourseCategory>().Property(item => item.DescriptionVi).HasMaxLength(500);
        modelBuilder.Entity<CourseCategory>().Property(item => item.DescriptionEn).HasMaxLength(500);
        modelBuilder.Entity<CourseCategory>().HasIndex(item => new { item.Kind, item.Framework, item.Code }).IsUnique();
        modelBuilder.Entity<Course>().ToTable("courses");
        modelBuilder.Entity<Unit>().ToTable("units");
        modelBuilder.Entity<Lesson>().ToTable("lessons");
        modelBuilder.Entity<Vocabulary>().ToTable("vocabularies");
        modelBuilder.Entity<Vocabulary>().Property(vocabulary => vocabulary.PartOfSpeech).HasMaxLength(60);
        modelBuilder.Entity<Deck>().ToTable("decks");
        modelBuilder.Entity<DeckTag>().ToTable("deck_tags");
        modelBuilder.Entity<Card>().ToTable("cards");
        modelBuilder.Entity<Card>().Property(card => card.PartOfSpeech).HasMaxLength(60);
        modelBuilder.Entity<CardSchedule>().ToTable("card_schedules");
        modelBuilder.Entity<CardLessonSource>().ToTable("card_lesson_sources");
        modelBuilder.Entity<CardLessonSource>().HasIndex(item => new { item.UserId, item.LessonId, item.CardId }).IsUnique();
        modelBuilder.Entity<ReviewEvent>().ToTable("review_events");
        modelBuilder.Entity<Question>().ToTable("questions");
        modelBuilder.Entity<QuestionOption>().ToTable("question_options");
        modelBuilder.Entity<QuizAttempt>().ToTable("quiz_attempts");
        modelBuilder.Entity<QuizAnswer>().ToTable("quiz_answers");
        modelBuilder.Entity<LessonProgress>().ToTable("lesson_progresses");
        modelBuilder.Entity<LessonStepProgress>().ToTable("lesson_step_progresses");
        modelBuilder.Entity<LearningSession>().ToTable("learning_sessions");
        modelBuilder.Entity<LearningSession>().Property(item => item.ContentVersion).HasMaxLength(64);
        modelBuilder.Entity<LearningSession>().HasIndex(item => new { item.UserId, item.LessonId }).IsUnique();
        modelBuilder.Entity<LearningEvidence>().ToTable("learning_evidences");
        modelBuilder.Entity<LearningEvidence>().Property(item => item.StepId).HasMaxLength(100);
        modelBuilder.Entity<LearningEvidence>().Property(item => item.StepType).HasMaxLength(24);
        modelBuilder.Entity<LearningEvidence>().Property(item => item.Outcome).HasMaxLength(24);
        modelBuilder.Entity<LearningEvidence>().Property(item => item.ContentVersion).HasMaxLength(64);
        modelBuilder.Entity<LearningEvidence>().HasIndex(item => new { item.UserId, item.LessonId, item.StepId }).IsUnique();
        modelBuilder.Entity<LessonStepProgress>().Property(item => item.StepId).HasMaxLength(100);
        modelBuilder.Entity<LessonStepProgress>().Property(item => item.Status).HasMaxLength(24);
        modelBuilder.Entity<LessonStepProgress>().Property(item => item.ActivityId).HasMaxLength(100);
        modelBuilder.Entity<LessonStepProgress>().Property(item => item.DraftJson).HasColumnType("longtext");
        modelBuilder.Entity<LessonStepProgress>().HasIndex(item => new { item.UserId, item.LessonId, item.StepId }).IsUnique();
        modelBuilder.Entity<CourseEnrollment>().ToTable("course_enrollments");
        modelBuilder.Entity<DailyLearningStats>().ToTable("daily_learning_stats");
        modelBuilder.Entity<UserGoal>().ToTable("user_goals");
        modelBuilder.Entity<SyncOperation>().ToTable("sync_operations");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.StudyTargetJson)
            .HasColumnType("longtext");

        modelBuilder.Entity<WritingSubmission>()
            .Property(submission => submission.PromptId)
            .HasMaxLength(200);

        modelBuilder.Entity<WritingSubmission>()
            .HasIndex(submission => new { submission.UserId, submission.PromptId, submission.CreatedAt });
        modelBuilder.Entity<WritingSubmission>()
            .HasIndex(submission => new { submission.UserId, submission.ClientSubmissionId }).IsUnique();
        modelBuilder.Entity<PlacementAttempt>()
            .HasIndex(attempt => new { attempt.UserId, attempt.CourseId, attempt.CreatedAt });
        modelBuilder.Entity<PlacementAttempt>().Property(attempt => attempt.Status).HasMaxLength(20);
        modelBuilder.Entity<PlacementAudio>()
            .HasIndex(audio => new { audio.UserId, audio.CourseId, audio.QuestionId, audio.FileName });
        modelBuilder.Entity<PlacementAudio>().Property(audio => audio.QuestionId).HasMaxLength(200);
        modelBuilder.Entity<PlacementAudio>().Property(audio => audio.FileName).HasMaxLength(128);

        modelBuilder.Entity<Course>()
            .Property(course => course.Domain)
            .HasMaxLength(50);

        modelBuilder.Entity<DeckTag>()
            .HasIndex(dt => new { dt.DeckId, dt.Tag })
            .IsUnique();

        modelBuilder.Entity<CardSchedule>()
            .HasIndex(cs => new { cs.UserId, cs.CardId })
            .IsUnique();

        modelBuilder.Entity<ReviewEvent>()
            .HasIndex(re => new { re.UserId, re.ClientEventId })
            .IsUnique();

        modelBuilder.Entity<LessonProgress>()
            .HasIndex(lp => new { lp.UserId, lp.LessonId })
            .IsUnique();
        modelBuilder.Entity<QuizAttempt>().HasIndex(item => new { item.UserId, item.ClientAttemptId }).IsUnique();
        modelBuilder.Entity<LessonProgress>().Property(item => item.CurrentStepId).HasMaxLength(100);
        modelBuilder.Entity<LessonStepProgress>().Property(item => item.Revision).IsConcurrencyToken();

        modelBuilder.Entity<CourseEnrollment>()
            .HasIndex(ce => new { ce.UserId, ce.CourseId })
            .IsUnique();

        modelBuilder.Entity<DailyLearningStats>()
            .HasIndex(dls => new { dls.UserId, dls.LocalDate })
            .IsUnique();

        modelBuilder.Entity<SyncOperation>()
            .HasIndex(so => new { so.UserId, so.ClientOperationId })
            .IsUnique();

        modelBuilder.Entity<CardSchedule>()
            .HasOne(cs => cs.User)
            .WithMany(u => u.CardSchedules)
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReviewEvent>()
            .HasOne(re => re.User)
            .WithMany(u => u.ReviewEvents)
            .HasForeignKey(re => re.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LessonProgress>()
            .HasOne(lp => lp.User)
            .WithMany(u => u.LessonProgresses)
            .HasForeignKey(lp => lp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CourseEnrollment>()
            .HasOne(ce => ce.User)
            .WithMany(u => u.CourseEnrollments)
            .HasForeignKey(ce => ce.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyLearningStats>()
            .HasOne(dls => dls.User)
            .WithMany(u => u.DailyLearningStats)
            .HasForeignKey(dls => dls.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SyncOperation>()
            .HasOne(so => so.User)
            .WithMany(u => u.SyncOperations)
            .HasForeignKey(so => so.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
