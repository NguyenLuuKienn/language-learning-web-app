using System.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Data;

public static class LegacySchemaUpgrade
{
    public static async Task EnsureWritingSubmissionKeyAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsMySql() && !context.Database.IsSqlite()) return;
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = context.Database.IsMySql()
                ? "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'writing_submissions' AND COLUMN_NAME = 'ClientSubmissionId'"
                : "SELECT COUNT(*) FROM pragma_table_info('writing_submissions') WHERE name = 'ClientSubmissionId'";
            if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
            {
                if (context.Database.IsMySql())
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE writing_submissions ADD COLUMN ClientSubmissionId CHAR(36) COLLATE ascii_general_ci NULL", cancellationToken);
                else
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE writing_submissions ADD COLUMN ClientSubmissionId TEXT NULL", cancellationToken);
            }
            if (context.Database.IsMySql())
            {
                command.CommandText = "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'writing_submissions' AND INDEX_NAME = 'IX_writing_submissions_UserId_ClientSubmissionId'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                    await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_writing_submissions_UserId_ClientSubmissionId ON writing_submissions (UserId, ClientSubmissionId)", cancellationToken);
            }
            else
                await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_writing_submissions_UserId_ClientSubmissionId ON writing_submissions (UserId, ClientSubmissionId)", cancellationToken);
        }
        finally { if (closeAfter) await connection.CloseAsync(); }
    }

    public static async Task EnsureSkillPracticeEventsAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS skill_practice_events (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, ClientAttemptId CHAR(36) COLLATE ascii_general_ci NOT NULL, CourseId CHAR(36) COLLATE ascii_general_ci NOT NULL, Kind VARCHAR(20) NOT NULL, ActivityId VARCHAR(100) NOT NULL, AnswerIndex INT NULL, Practiced tinyint(1) NOT NULL, Correct tinyint(1) NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_skill_practice_events_UserId_ClientAttemptId (UserId, ClientAttemptId))", cancellationToken);
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS skill_practice_events (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, ClientAttemptId TEXT NOT NULL, CourseId TEXT NOT NULL, Kind TEXT NOT NULL, ActivityId TEXT NOT NULL, AnswerIndex INTEGER NULL, Practiced INTEGER NOT NULL, Correct INTEGER NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_skill_practice_events_UserId_ClientAttemptId ON skill_practice_events (UserId, ClientAttemptId)", cancellationToken);
        }
    }

    public static async Task EnsureQuizAttemptKeyAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsMySql() && !context.Database.IsSqlite()) return;
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = context.Database.IsMySql()
                ? "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'quiz_attempts' AND COLUMN_NAME = 'ClientAttemptId'"
                : "SELECT COUNT(*) FROM pragma_table_info('quiz_attempts') WHERE name = 'ClientAttemptId'";
            if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
            {
                if (context.Database.IsMySql())
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE quiz_attempts ADD COLUMN ClientAttemptId CHAR(36) COLLATE ascii_general_ci NULL", cancellationToken);
                else
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE quiz_attempts ADD COLUMN ClientAttemptId TEXT NULL", cancellationToken);
            }
            if (context.Database.IsMySql())
            {
                command.CommandText = "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'quiz_attempts' AND INDEX_NAME = 'IX_quiz_attempts_UserId_ClientAttemptId'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                    await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_quiz_attempts_UserId_ClientAttemptId ON quiz_attempts (UserId, ClientAttemptId)", cancellationToken);
            }
            else
                await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_quiz_attempts_UserId_ClientAttemptId ON quiz_attempts (UserId, ClientAttemptId)", cancellationToken);
        }
        finally { if (closeAfter) await connection.CloseAsync(); }
    }

    public static async Task EnsureReviewObservationColumnsAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsMySql() && !context.Database.IsSqlite()) return;
        var table = context.Database.IsMySql() ? "information_schema.COLUMNS" : "pragma_table_info('review_events')";
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            foreach (var column in new[] { "WasDue", "PreviousIntervalDays" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = context.Database.IsMySql()
                    ? $"SELECT COUNT(*) FROM {table} WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'review_events' AND COLUMN_NAME = '{column}'"
                    : $"SELECT COUNT(*) FROM {table} WHERE name = '{column}'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) != 0) continue;
                if (column == "WasDue" && context.Database.IsMySql())
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE review_events ADD COLUMN WasDue tinyint(1) NULL", cancellationToken);
                else if (column == "WasDue")
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE review_events ADD COLUMN WasDue INTEGER NULL", cancellationToken);
                else if (context.Database.IsMySql())
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE review_events ADD COLUMN PreviousIntervalDays DOUBLE NULL", cancellationToken);
                else
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE review_events ADD COLUMN PreviousIntervalDays REAL NULL", cancellationToken);
            }
        }
        finally { if (closeAfter) await connection.CloseAsync(); }
    }

    public static async Task EnsureLearningJourneyRecordsAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS learning_sessions (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, LessonId CHAR(36) COLLATE ascii_general_ci NOT NULL, ContentVersion VARCHAR(64) NOT NULL, StartedAt DATETIME(6) NOT NULL, LastActiveAt DATETIME(6) NOT NULL, CompletedAt DATETIME(6) NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_learning_sessions_UserId_LessonId (UserId, LessonId))", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS learning_evidences (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, LessonId CHAR(36) COLLATE ascii_general_ci NOT NULL, StepId VARCHAR(100) NOT NULL, StepType VARCHAR(24) NOT NULL, Outcome VARCHAR(24) NOT NULL, ContentVersion VARCHAR(64) NOT NULL, ObservedAt DATETIME(6) NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_learning_evidences_UserId_LessonId_StepId (UserId, LessonId, StepId))", cancellationToken);
        }
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS learning_sessions (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, LessonId TEXT NOT NULL, ContentVersion TEXT NOT NULL, StartedAt TEXT NOT NULL, LastActiveAt TEXT NOT NULL, CompletedAt TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_learning_sessions_UserId_LessonId ON learning_sessions (UserId, LessonId)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS learning_evidences (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, LessonId TEXT NOT NULL, StepId TEXT NOT NULL, StepType TEXT NOT NULL, Outcome TEXT NOT NULL, ContentVersion TEXT NOT NULL, ObservedAt TEXT NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_learning_evidences_UserId_LessonId_StepId ON learning_evidences (UserId, LessonId, StepId)", cancellationToken);
        }
    }

    public static async Task EnsureCardLessonSourcesAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS card_lesson_sources (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, LessonId CHAR(36) COLLATE ascii_general_ci NOT NULL, CardId CHAR(36) COLLATE ascii_general_ci NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_card_lesson_sources_UserId_LessonId_CardId (UserId, LessonId, CardId), INDEX IX_card_lesson_sources_CardId (CardId), CONSTRAINT FK_card_lesson_sources_cards_CardId FOREIGN KEY (CardId) REFERENCES cards(Id) ON DELETE CASCADE)", cancellationToken);
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS card_lesson_sources (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, LessonId TEXT NOT NULL, CardId TEXT NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL, FOREIGN KEY (CardId) REFERENCES cards(Id) ON DELETE CASCADE)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_card_lesson_sources_UserId_LessonId_CardId ON card_lesson_sources (UserId, LessonId, CardId)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_card_lesson_sources_CardId ON card_lesson_sources (CardId)", cancellationToken);
        }
        else return;

        var oldTags = await context.DeckTags.AsNoTracking()
            .Where(tag => tag.Tag.StartsWith("vocab-lesson:"))
            .Select(tag => new { tag.DeckId, tag.Tag }).ToListAsync(cancellationToken);
        if (oldTags.Count == 0) return;
        var deckIds = oldTags.Select(tag => tag.DeckId).Distinct().ToArray();
        var cards = await context.Cards.AsNoTracking().Where(card => deckIds.Contains(card.DeckId))
            .Select(card => new { card.Id, card.DeckId, card.Deck.OwnerId }).ToListAsync(cancellationToken);
        var cardIds = cards.Select(card => card.Id).ToArray();
        var existing = (await context.CardLessonSources.AsNoTracking().Where(source => cardIds.Contains(source.CardId))
            .Select(source => new { source.LessonId, source.CardId }).ToListAsync(cancellationToken))
            .Select(source => (source.LessonId, source.CardId)).ToHashSet();
        foreach (var tag in oldTags)
        {
            if (!Guid.TryParse(tag.Tag["vocab-lesson:".Length..], out var lessonId)) continue;
            foreach (var card in cards.Where(card => card.DeckId == tag.DeckId))
                if (existing.Add((lessonId, card.Id)))
                    context.CardLessonSources.Add(new LanguageLearning.Domain.Entities.CardLessonSource
                    {
                        Id = Guid.NewGuid(), UserId = card.OwnerId, LessonId = lessonId, CardId = card.Id
                    });
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureLessonJourneyProgressAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS lesson_step_progresses (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, LessonId CHAR(36) COLLATE ascii_general_ci NOT NULL, StepId VARCHAR(100) NOT NULL, Status VARCHAR(24) NOT NULL, ActivityId VARCHAR(100) NULL, `Cursor` INT NOT NULL, Revision INT NOT NULL DEFAULT 0, DraftJson LONGTEXT NOT NULL, LastAccessedAt DATETIME(6) NOT NULL, CompletedAt DATETIME(6) NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_lesson_step_progresses_UserId_LessonId_StepId (UserId, LessonId, StepId))",
                cancellationToken);
            var connection = context.Database.GetDbConnection();
            var closeAfter = connection.State != ConnectionState.Open;
            if (closeAfter) await connection.OpenAsync(cancellationToken);
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'lesson_progresses' AND COLUMN_NAME = 'CurrentStepId'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE lesson_progresses ADD COLUMN CurrentStepId VARCHAR(100) NULL", cancellationToken);
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'lesson_step_progresses' AND COLUMN_NAME = 'Revision'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE lesson_step_progresses ADD COLUMN Revision INT NOT NULL DEFAULT 0", cancellationToken);
            }
            finally { if (closeAfter) await connection.CloseAsync(); }
        }
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS lesson_step_progresses (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, LessonId TEXT NOT NULL, StepId TEXT NOT NULL, Status TEXT NOT NULL, ActivityId TEXT NULL, Cursor INTEGER NOT NULL, Revision INTEGER NOT NULL DEFAULT 0, DraftJson TEXT NOT NULL, LastAccessedAt TEXT NOT NULL, CompletedAt TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_lesson_step_progresses_UserId_LessonId_StepId ON lesson_step_progresses (UserId, LessonId, StepId)", cancellationToken);
            var connection = context.Database.GetDbConnection();
            var closeAfter = connection.State != ConnectionState.Open;
            if (closeAfter) await connection.OpenAsync(cancellationToken);
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('lesson_progresses') WHERE name = 'CurrentStepId'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE lesson_progresses ADD COLUMN CurrentStepId TEXT NULL", cancellationToken);
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('lesson_step_progresses') WHERE name = 'Revision'";
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                    await context.Database.ExecuteSqlRawAsync("ALTER TABLE lesson_step_progresses ADD COLUMN Revision INTEGER NOT NULL DEFAULT 0", cancellationToken);
            }
            finally { if (closeAfter) await connection.CloseAsync(); }
        }
    }

    public static async Task EnsurePracticeTablesAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS skill_practice_progresses (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, CourseId CHAR(36) COLLATE ascii_general_ci NOT NULL, Kind VARCHAR(20) NOT NULL, ActivityId VARCHAR(100) NOT NULL, Status VARCHAR(20) NOT NULL, Attempts INT NOT NULL, CorrectAttempts INT NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_skill_practice_progresses_UserId_CourseId_Kind_ActivityId (UserId, CourseId, Kind, ActivityId))",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS special_practice_attempts (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, CourseId CHAR(36) COLLATE ascii_general_ci NOT NULL, ContentId CHAR(36) COLLATE ascii_general_ci NOT NULL, Kind VARCHAR(20) NOT NULL, ContentSnapshotJson LONGTEXT NOT NULL, AnswersJson LONGTEXT NOT NULL, Status VARCHAR(20) NOT NULL, ObjectiveScore INT NOT NULL, ObjectiveMaxScore INT NOT NULL, SubjectiveScore INT NOT NULL, SubjectiveMaxScore INT NOT NULL, EvaluationJson LONGTEXT NOT NULL, ReviewedAt DATETIME(6) NULL, SubmittedAt DATETIME(6) NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, INDEX IX_special_practice_attempts_UserId_CourseId_ContentId_Status (UserId, CourseId, ContentId, Status))",
                cancellationToken);
        }
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS skill_practice_progresses (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CourseId TEXT NOT NULL, Kind TEXT NOT NULL, ActivityId TEXT NOT NULL, Status TEXT NOT NULL, Attempts INTEGER NOT NULL, CorrectAttempts INTEGER NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_skill_practice_progresses_UserId_CourseId_Kind_ActivityId ON skill_practice_progresses (UserId, CourseId, Kind, ActivityId)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS special_practice_attempts (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CourseId TEXT NOT NULL, ContentId TEXT NOT NULL, Kind TEXT NOT NULL, ContentSnapshotJson TEXT NOT NULL, AnswersJson TEXT NOT NULL, Status TEXT NOT NULL, ObjectiveScore INTEGER NOT NULL, ObjectiveMaxScore INTEGER NOT NULL, SubjectiveScore INTEGER NOT NULL, SubjectiveMaxScore INTEGER NOT NULL, EvaluationJson TEXT NOT NULL, ReviewedAt TEXT NULL, SubmittedAt TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_special_practice_attempts_UserId_CourseId_ContentId_Status ON special_practice_attempts (UserId, CourseId, ContentId, Status)",
                cancellationToken);
        }
    }

    public static async Task EnsureCourseCategoriesTableAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS course_categories (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, Kind VARCHAR(20) NOT NULL, Framework VARCHAR(20) NOT NULL, Code VARCHAR(50) NOT NULL, TitleVi VARCHAR(150) NOT NULL, TitleEn VARCHAR(150) NOT NULL, DescriptionVi VARCHAR(500) NOT NULL, DescriptionEn VARCHAR(500) NOT NULL, SortOrder INT NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, UNIQUE INDEX IX_course_categories_Kind_Framework_Code (Kind, Framework, Code))",
                cancellationToken);
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS course_categories (Id TEXT NOT NULL PRIMARY KEY, Kind TEXT NOT NULL, Framework TEXT NOT NULL, Code TEXT NOT NULL, TitleVi TEXT NOT NULL, TitleEn TEXT NOT NULL, DescriptionVi TEXT NOT NULL, DescriptionEn TEXT NOT NULL, SortOrder INTEGER NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_course_categories_Kind_Framework_Code ON course_categories (Kind, Framework, Code)",
                cancellationToken);
        }
    }

    public static async Task EnsureVocabularySessionTableAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS vocabulary_sessions (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, LessonId CHAR(36) COLLATE ascii_general_ci NOT NULL, GroupIndex INT NOT NULL, StepsJson LONGTEXT NOT NULL, ResultsJson LONGTEXT NOT NULL, Position INT NOT NULL, CompletedAt DATETIME(6) NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, INDEX IX_vocabulary_sessions_UserId_LessonId_CompletedAt (UserId, LessonId, CompletedAt))",
                cancellationToken);
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS vocabulary_sessions (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, LessonId TEXT NOT NULL, GroupIndex INTEGER NOT NULL, StepsJson TEXT NOT NULL, ResultsJson TEXT NOT NULL, Position INTEGER NOT NULL, CompletedAt TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_vocabulary_sessions_UserId_LessonId_CompletedAt ON vocabulary_sessions (UserId, LessonId, CompletedAt)",
                cancellationToken);
        }
    }

    public static async Task EnsureNotebookTableAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS notebook_entries (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, Text VARCHAR(500) NOT NULL, Note VARCHAR(5000) NOT NULL, LanguageCode VARCHAR(12) NOT NULL, SourcePath VARCHAR(500) NOT NULL, Context VARCHAR(1000) NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, INDEX IX_notebook_entries_UserId_CreatedAt (UserId, CreatedAt))",
                cancellationToken);
        }
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS notebook_entries (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, Text TEXT NOT NULL, Note TEXT NOT NULL, LanguageCode TEXT NOT NULL, SourcePath TEXT NOT NULL, Context TEXT NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)",
                cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_notebook_entries_UserId_CreatedAt ON notebook_entries (UserId, CreatedAt)",
                cancellationToken);
        }
    }

    public static async Task EnsureVocabularyDetailColumnsAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsMySql()) return;
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            foreach (var column in new[] { "MeaningsJson", "PartOfSpeech" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'vocabularies' AND COLUMN_NAME = @column";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@column";
                parameter.Value = column;
                command.Parameters.Add(parameter);
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                {
                    if (column == "MeaningsJson")
                    {
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE vocabularies ADD COLUMN MeaningsJson LONGTEXT NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("UPDATE vocabularies SET MeaningsJson = '[]' WHERE MeaningsJson IS NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE vocabularies MODIFY COLUMN MeaningsJson LONGTEXT NOT NULL", cancellationToken);
                    }
                    else
                    {
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE vocabularies ADD COLUMN PartOfSpeech VARCHAR(60) NOT NULL DEFAULT ''", cancellationToken);
                    }
                }
            }
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }

    public static async Task EnsureCardDetailColumnsAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsMySql()) return;
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            foreach (var column in new[] { "Phonetic", "PartOfSpeech", "MeaningsJson", "Example" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'cards' AND COLUMN_NAME = @column";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@column";
                parameter.Value = column;
                command.Parameters.Add(parameter);
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) != 0) continue;
                switch (column)
                {
                    case "Phonetic":
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards ADD COLUMN Phonetic LONGTEXT NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("UPDATE cards SET Phonetic = '' WHERE Phonetic IS NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards MODIFY COLUMN Phonetic LONGTEXT NOT NULL", cancellationToken);
                        break;
                    case "PartOfSpeech":
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards ADD COLUMN PartOfSpeech VARCHAR(60) NOT NULL DEFAULT ''", cancellationToken);
                        break;
                    case "Example":
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards ADD COLUMN Example LONGTEXT NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("UPDATE cards SET Example = '' WHERE Example IS NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards MODIFY COLUMN Example LONGTEXT NOT NULL", cancellationToken);
                        break;
                    case "MeaningsJson":
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards ADD COLUMN MeaningsJson LONGTEXT NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("UPDATE cards SET MeaningsJson = '[]' WHERE MeaningsJson IS NULL", cancellationToken);
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE cards MODIFY COLUMN MeaningsJson LONGTEXT NOT NULL", cancellationToken);
                        break;
                }
            }
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }

    public static async Task EnsurePlacementReviewTablesAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS placement_attempts (Id CHAR(36) NOT NULL PRIMARY KEY, UserId CHAR(36) NOT NULL, CourseId CHAR(36) NOT NULL, QuestionsJson LONGTEXT NOT NULL, AnswersJson LONGTEXT NOT NULL, AudioFilesJson LONGTEXT NOT NULL, ScoresJson LONGTEXT NOT NULL, Status VARCHAR(20) NOT NULL, Feedback LONGTEXT NOT NULL, ReviewedBy CHAR(36) NULL, ReviewedAt DATETIME(6) NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, INDEX IX_placement_attempts_UserId_CourseId_CreatedAt (UserId, CourseId, CreatedAt))", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS placement_audios (Id CHAR(36) NOT NULL PRIMARY KEY, UserId CHAR(36) NOT NULL, CourseId CHAR(36) NOT NULL, QuestionId VARCHAR(200) NOT NULL, FileName VARCHAR(128) NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, INDEX IX_placement_audios_UserId_CourseId_QuestionId_FileName (UserId, CourseId, QuestionId, FileName))", cancellationToken);
        }
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS placement_attempts (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CourseId TEXT NOT NULL, QuestionsJson TEXT NOT NULL, AnswersJson TEXT NOT NULL, AudioFilesJson TEXT NOT NULL, ScoresJson TEXT NOT NULL, Status TEXT NOT NULL, Feedback TEXT NOT NULL, ReviewedBy TEXT NULL, ReviewedAt TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_placement_attempts_UserId_CourseId_CreatedAt ON placement_attempts (UserId, CourseId, CreatedAt)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS placement_audios (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CourseId TEXT NOT NULL, QuestionId TEXT NOT NULL, FileName TEXT NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_placement_audios_UserId_CourseId_QuestionId_FileName ON placement_audios (UserId, CourseId, QuestionId, FileName)", cancellationToken);
        }
    }

    public static async Task EnsureWritingSubmissionsTableAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsMySql())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS writing_submissions (Id CHAR(36) COLLATE ascii_general_ci NOT NULL PRIMARY KEY, UserId CHAR(36) COLLATE ascii_general_ci NOT NULL, PromptId VARCHAR(200) NOT NULL, Text LONGTEXT NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NULL, INDEX IX_writing_submissions_UserId_PromptId_CreatedAt (UserId, PromptId, CreatedAt), CONSTRAINT FK_writing_submissions_users_UserId FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE)", cancellationToken);
        }
        else if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS writing_submissions (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, PromptId TEXT NOT NULL, Text TEXT NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NULL, FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE)", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_writing_submissions_UserId_PromptId_CreatedAt ON writing_submissions (UserId, PromptId, CreatedAt)", cancellationToken);
        }
    }

    public static async Task EnsureStudyTargetColumnAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (context.Database.IsSqlite())
        {
            var sqliteConnection = context.Database.GetDbConnection();
            var closeSqlite = sqliteConnection.State != ConnectionState.Open;
            if (closeSqlite) await sqliteConnection.OpenAsync(cancellationToken);
            try
            {
                using var command = sqliteConnection.CreateCommand();
                command.CommandText = "PRAGMA table_info(users)";
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var exists = false;
                while (await reader.ReadAsync(cancellationToken))
                    exists |= string.Equals(reader.GetString(1), "StudyTargetJson", StringComparison.OrdinalIgnoreCase);
                await reader.CloseAsync();
                if (!exists) await context.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN StudyTargetJson TEXT NULL", cancellationToken);
            }
            finally
            {
                if (closeSqlite) await sqliteConnection.CloseAsync();
            }
            return;
        }
        if (!context.Database.IsMySql()) return;
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'StudyTargetJson'";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (count == 0)
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN StudyTargetJson LONGTEXT NULL", cancellationToken);
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }

    public static async Task EnsureCourseMaterialColumnsAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsMySql()) return;

        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            foreach (var column in new[] { "Domain", "MaterialJson" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'courses' AND COLUMN_NAME = @column";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@column";
                parameter.Value = column;
                command.Parameters.Add(parameter);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
                if (count == 0)
                {
                    if (column == "Domain")
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE courses ADD COLUMN Domain VARCHAR(50) NOT NULL DEFAULT 'general'", cancellationToken);
                    else
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE courses ADD COLUMN MaterialJson LONGTEXT NULL", cancellationToken);
                }
            }
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }
}
