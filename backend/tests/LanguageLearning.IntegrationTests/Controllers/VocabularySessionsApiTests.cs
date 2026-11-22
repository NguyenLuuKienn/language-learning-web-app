using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.DTOs.VocabularyPractice;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class VocabularySessionsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public VocabularySessionsApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task LegacyExamples_TwoWordFinalGroup_RemainsLearnable()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        var lessonId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            db.Courses.Add(new Course
            {
                Id = Guid.NewGuid(), LanguageId = language.Id, Title = "Legacy words", Level = "A1",
                Domain = "general", Status = ContentStatus.Published,
                Units = [new Unit { Id = Guid.NewGuid(), Title = "Fruit", SortOrder = 1,
                    Lessons = [new Lesson { Id = lessonId, Title = "Two words", SortOrder = 1, Status = ContentStatus.Published,
                        Vocabularies = [
                            new Vocabulary { Id = Guid.NewGuid(), LanguageId = language.Id, Term = "apple", Meaning = "táo", Example = "I eat an apple." },
                            new Vocabulary { Id = Guid.NewGuid(), LanguageId = language.Id, Term = "orange", Meaning = "cam", Example = "I eat an orange." }
                        ] }] }]
            });
            await db.SaveChangesAsync();
        }

        var choices = await learner.GetFromJsonAsync<List<VocabularyLessonChoice>>("/api/v1/vocabulary-sessions/lessons");
        Assert.Contains(choices!, item => item.LessonId == lessonId && item.AvailableGroups == 1);
        var response = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions", new StartVocabularySessionRequest { LessonId = lessonId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<VocabularySessionResponse>();
        Assert.Equal(10, session!.TotalSteps);
        Assert.Equal(HttpStatusCode.BadRequest, (await learner.PostAsync(
            "/api/v1/vocabulary-sessions/" + session.Id + "/complete-perfect-round", null)).StatusCode);
        var meanings = new Dictionary<string, string> { ["apple"] = "t\u00e1o", ["orange"] = "cam" };
        while (session.Step?.Round != 2)
        {
            var step = session.Step!;
            var answer = step.Kind switch
            {
                "listenMeaning" => meanings[step.Word],
                "cloze" => step.Prompt.Contains("eat") ? "apple" : "orange",
                "dictation" => step.Word,
                _ => ""
            };
            var answered = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions/" + session.Id + "/answers",
                new VocabularyPracticeAnswerRequest { StepId = step.Id, Answer = answer,
                    Matches = step.Kind == "matching" ? meanings : null });
            Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
            var result = await answered.Content.ReadFromJsonAsync<VocabularyAnswerResponse>();
            Assert.NotNull(result);
            session = result!.Session;
        }
        if (session.CanCompleteAfterRoundOne)
        {
            var skipped = await learner.PostAsync("/api/v1/vocabulary-sessions/" + session.Id + "/complete-perfect-round", null);
            Assert.Equal(HttpStatusCode.OK, skipped.StatusCode);
            session = (await skipped.Content.ReadFromJsonAsync<VocabularySessionResponse>())!;
        }
        else
        {
            while (!session.Completed)
            {
                var step = session.Step!;
                var roundResponse = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions/" + session.Id + "/answers",
                    new VocabularyPracticeAnswerRequest { StepId = step.Id, Answer = step.Kind == "dictation" ? step.Word :
                        step.Kind == "listenMeaning" ? meanings[step.Word] : step.Kind == "cloze" ? "apple" : "",
                        Matches = step.Kind == "matching" ? meanings : null });
                Assert.Equal(HttpStatusCode.OK, roundResponse.StatusCode);
                session = (await roundResponse.Content.ReadFromJsonAsync<VocabularyAnswerResponse>())!.Session;
            }
        }
        Assert.True(session.Completed);
        var completedChoices = await learner.GetFromJsonAsync<List<VocabularyLessonChoice>>("/api/v1/vocabulary-sessions/lessons");
        Assert.Contains(completedChoices!, item => item.LessonId == lessonId &&
            item.AvailableGroups == 1 && item.CompletedGroups == 1 && item.ActiveSessionId == null);
        Assert.Equal(HttpStatusCode.BadRequest, (await learner.PostAsJsonAsync(
            "/api/v1/vocabulary-sessions", new StartVocabularySessionRequest { LessonId = lessonId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await learner.PostAsync(
            "/api/v1/vocabulary-sessions/" + session.Id + "/complete-perfect-round", null)).StatusCode);
    }

    [Fact]
    public async Task ThreeWords_TwoRounds_ResumeAndOwnerScope()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        Guid lessonId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            var cards = new[]
            {
                new { front = "apply", back = "nộp đơn", meanings = new[] { "nộp đơn" }, phonetic = "/əˈplaɪ/", partOfSpeech = "verb",
                    example = "I apply for work.", exampleTranslation = "Tôi nộp đơn xin việc.", clozeSentence = "I ___ for work.", acceptedAnswers = new[] { "apply" }, audioUrl = "" },
                new { front = "skill", back = "kỹ năng", meanings = new[] { "kỹ năng" }, phonetic = "/skɪl/", partOfSpeech = "noun",
                    example = "This skill matters.", exampleTranslation = "Kỹ năng này quan trọng.", clozeSentence = "This ___ matters.", acceptedAnswers = new[] { "skill" }, audioUrl = "" },
                new { front = "team", back = "đội nhóm", meanings = new[] { "đội nhóm" }, phonetic = "/tiːm/", partOfSpeech = "noun",
                    example = "Our team works well.", exampleTranslation = "Đội chúng tôi làm việc tốt.", clozeSentence = "Our ___ works well.", acceptedAnswers = new[] { "team" }, audioUrl = "" }
            };
            db.Courses.Add(new Course
            {
                Id = Guid.NewGuid(), LanguageId = language.Id, Title = "Practice A1", Level = "A1",
                Domain = "general", Status = ContentStatus.Published,
                MaterialJson = JsonSerializer.Serialize(new
                {
                    units = new[] { new { lessons = new[] { new { id = lessonId, flashcards = cards } } } }
                }),
                Units = [new Unit { Id = Guid.NewGuid(), Title = "Work", SortOrder = 1,
                    Lessons = [new Lesson { Id = lessonId, Title = "Interview words", SortOrder = 1, Status = ContentStatus.Published }] }]
            });
            await db.SaveChangesAsync();
        }

        var choices = await learner.GetFromJsonAsync<List<VocabularyLessonChoice>>("/api/v1/vocabulary-sessions/lessons");
        Assert.Contains(choices!, item => item.LessonId == lessonId && item.AvailableGroups == 1);
        var roadmap = await learner.GetFromJsonAsync<LearningRoadmapResponse>("/api/v1/progress/roadmap");
        Assert.NotNull(roadmap);
        Assert.NotNull(roadmap.NextLessonId);
        Assert.Contains(roadmap.Courses.SelectMany(course => course.Units).SelectMany(unit => unit.Lessons),
            item => item.Id == lessonId && item.Status == "NotStarted");
        var forgedCompletion = await learner.PatchAsJsonAsync("/api/v1/progress/lessons/" + lessonId,
            new { status = "Completed" });
        Assert.Equal(HttpStatusCode.BadRequest, forgedCompletion.StatusCode);
        var resume = await learner.GetFromJsonAsync<ResumeLearningResponse>("/api/v1/progress/resume");
        Assert.NotNull(resume);
        Assert.Equal(roadmap.NextLessonId, resume.LessonId);
        var created = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions", new StartVocabularySessionRequest { LessonId = lessonId });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var session = await created.Content.ReadFromJsonAsync<VocabularySessionResponse>();
        Assert.NotNull(session);
        Assert.Equal(11, session.TotalSteps);
        Assert.Equal("introduction", session.Step?.Kind);
        var inProgressChoices = await learner.GetFromJsonAsync<List<VocabularyLessonChoice>>("/api/v1/vocabulary-sessions/lessons");
        Assert.Contains(inProgressChoices!, item => item.LessonId == lessonId &&
            item.ActiveSessionId == session.Id && item.ActivePosition == 0 && item.ActiveTotalSteps == 11);
        Assert.DoesNotContain("acceptedAnswers", await created.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/v1/vocabulary-sessions/" + session.Id)).StatusCode);

        var firstStepId = session.Step!.Id;
        var first = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions/" + session.Id + "/answers",
            new VocabularyPracticeAnswerRequest { StepId = firstStepId });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        session = (await first.Content.ReadFromJsonAsync<VocabularyAnswerResponse>())!.Session;
        var duplicate = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions/" + session.Id + "/answers",
            new VocabularyPracticeAnswerRequest { StepId = firstStepId });
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(1, (await duplicate.Content.ReadFromJsonAsync<VocabularyAnswerResponse>())!.Session.Position);
        Assert.Equal(1, (await learner.GetFromJsonAsync<VocabularySessionResponse>("/api/v1/vocabulary-sessions/" + session.Id))!.Position);

        var meanings = new Dictionary<string, string> { ["apply"] = "nộp đơn", ["skill"] = "kỹ năng", ["team"] = "đội nhóm" };
        while (!session.Completed)
        {
            var step = session.Step!;
            if (step.Kind == "cloze")
            {
                Assert.Equal(string.Empty, step.Word);
                Assert.False(string.IsNullOrWhiteSpace(step.Meaning));
                Assert.False(string.IsNullOrWhiteSpace(step.ExampleTranslation));
            }
            if (step.Kind == "matching" && step.MatchWords.Count > 1)
                for (var index = 0; index < step.MatchWords.Count; index++)
                    Assert.NotEqual(meanings[step.MatchWords[index]], step.MatchMeanings[index]);
            var answer = step.Kind switch
            {
                "listenMeaning" => meanings[step.Word],
                "cloze" => step.Prompt.Contains("for work.") ? "apply" : step.Prompt.Contains("matters") ? "skill" : "team",
                "dictation" => step.Word,
                _ => ""
            };
            var response = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions/" + session.Id + "/answers",
                new VocabularyPracticeAnswerRequest { StepId = step.Id, Answer = answer,
                    Matches = step.Kind == "matching" ? meanings : null });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            session = (await response.Content.ReadFromJsonAsync<VocabularyAnswerResponse>())!.Session;
        }
        Assert.Equal(11, session.Position);
        Assert.True(session.Completed);
        var completedRoadmap = await learner.GetFromJsonAsync<LearningRoadmapResponse>("/api/v1/progress/roadmap");
        Assert.Contains(completedRoadmap!.Courses.SelectMany(course => course.Units).SelectMany(unit => unit.Lessons),
            item => item.Id == lessonId && item.Status != "Completed");
        var overview = await learner.GetFromJsonAsync<ReviewOverviewResponse>("/api/v1/reviews/overview");
        Assert.NotNull(overview);
        Assert.True(overview.LearningCount >= 3);
        var dueOnly = await learner.GetFromJsonAsync<DueCardsSummaryResponse>("/api/v1/reviews/due?dueOnly=true");
        Assert.NotNull(dueOnly);
        Assert.DoesNotContain(dueOnly.Cards, card => new[] { "apply", "skill", "team" }.Contains(card.Front));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var cards = await db.Cards.Where(card => card.Deck.OwnerId == user.Id &&
                card.Deck.Tags.Any(tag => tag.Tag == "vocab-lesson:" + lessonId)).ToListAsync();
            Assert.Equal(3, cards.Count);
            Assert.Equal(3, await db.CardLessonSources.CountAsync(source => source.UserId == user.Id &&
                source.LessonId == lessonId && cards.Select(card => card.Id).Contains(source.CardId)));
            Assert.Equal(3, await db.CardSchedules.CountAsync(schedule => schedule.UserId == user.Id &&
                cards.Select(card => card.Id).Contains(schedule.CardId)));
            var schedules = await db.CardSchedules.Where(schedule => schedule.UserId == user.Id &&
                cards.Select(card => card.Id).Contains(schedule.CardId)).ToListAsync();
            Assert.All(schedules, schedule =>
            {
                Assert.Null(schedule.LastReviewedAt);
                Assert.Equal(0, LanguageLearning.Application.Services.SrsState.FromJson(schedule.StateJson).Repetitions);
            });
        }
    }

    [Fact]
    public async Task SixWords_PerfectFirstRound_FinishesWholeLessonWithoutAnotherSession()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        var lessonId = Guid.NewGuid();
        var meanings = new Dictionary<string, string>
        {
            ["apple"] = "táo", ["orange"] = "cam", ["banana"] = "chuối",
            ["grape"] = "nho", ["lemon"] = "chanh", ["melon"] = "dưa"
        };
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            db.Courses.Add(new Course
            {
                Id = Guid.NewGuid(), LanguageId = language.Id, Title = "Six words", Level = "A1",
                Domain = "general", Status = ContentStatus.Published,
                Units = [new Unit { Id = Guid.NewGuid(), Title = "Fruit", SortOrder = 1,
                    Lessons = [new Lesson { Id = lessonId, Title = "Fruit words", SortOrder = 1,
                        Status = ContentStatus.Published,
                        Vocabularies = meanings.Select(pair => new Vocabulary
                        {
                            Id = Guid.NewGuid(), LanguageId = language.Id, Term = pair.Key,
                            Meaning = pair.Value, Example = "I like " + pair.Key + "."
                        }).ToList() }] }]
            });
            await db.SaveChangesAsync();
        }

        var created = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions",
            new StartVocabularySessionRequest { LessonId = lessonId });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var session = (await created.Content.ReadFromJsonAsync<VocabularySessionResponse>())!;
        Assert.Equal(22, session.TotalSteps);
        Assert.Equal(6, session.Words.Count);
        var clozeIndex = 0;
        while (session.Step?.Round != 2)
        {
            var step = session.Step!;
            var answer = step.Kind switch
            {
                "listenMeaning" => meanings[step.Word],
                "cloze" => session.Words[clozeIndex++ == 0 ? 1 : 4],
                "dictation" => step.Word,
                _ => ""
            };
            var response = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions/" + session.Id + "/answers",
                new VocabularyPracticeAnswerRequest { StepId = step.Id, Answer = answer,
                    Matches = step.Kind == "matching" ? step.MatchWords.ToDictionary(word => word, word => meanings[word]) : null });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<VocabularyAnswerResponse>())!;
            Assert.True(result.Correct);
            session = result.Session;
        }
        Assert.True(session.CanCompleteAfterRoundOne);
        var completed = await learner.PostAsync("/api/v1/vocabulary-sessions/" + session.Id + "/complete-perfect-round", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var choices = await learner.GetFromJsonAsync<List<VocabularyLessonChoice>>("/api/v1/vocabulary-sessions/lessons");
        Assert.Contains(choices!, choice => choice.LessonId == lessonId && choice.CompletedGroups == 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await learner.PostAsJsonAsync(
            "/api/v1/vocabulary-sessions", new StartVocabularySessionRequest { LessonId = lessonId })).StatusCode);
    }
}
