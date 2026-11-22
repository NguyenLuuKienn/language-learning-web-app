using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Application.DTOs.Quiz;
using LanguageLearning.Application.DTOs.VocabularyPractice;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class LessonJourneyApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public LessonJourneyApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GuidedCourse_QuizCompletion_UnlocksNextLesson_AndRetryIsIdempotent()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        var courseId = Guid.NewGuid();
        var firstLessonId = Guid.NewGuid();
        var nextLessonId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
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
                Id = courseId, LanguageId = language.Id, Title = "Quiz journey", Level = "A1", Domain = "general",
                Status = ContentStatus.Published,
                MaterialJson = JsonSerializer.Serialize(new { units = new[] { new { lessons = new object[]
                {
                    new { id = firstLessonId, summary = "Choose a greeting", quizQuestions = new[] { new { id = questionId, question = "Choose hello" } },
                        lessonSteps = new object[]
                        {
                            new { id = "intro", type = "introduction", title = "Introduction", order = 0, required = true, activityRefs = Array.Empty<string>(), completionPolicy = new { kind = "viewed" } },
                            new { id = "quiz", type = "quiz", title = "Quiz", order = 1, required = true, activityRefs = Array.Empty<string>(), completionPolicy = new { kind = "quiz_passed" } }
                        } },
                    new { id = nextLessonId, summary = "Next lesson", quizQuestions = Array.Empty<object>(),
                        lessonSteps = new object[] { new { id = "intro", type = "introduction", title = "Introduction", order = 0, required = true, activityRefs = Array.Empty<string>(), completionPolicy = new { kind = "viewed" } } } }
                } } } }),
                Units = [new Unit { Id = Guid.NewGuid(), Title = "Unit", SortOrder = 1,
                    Lessons =
                    [
                        new Lesson { Id = firstLessonId, Title = "First", SortOrder = 1, Status = ContentStatus.Published },
                        new Lesson { Id = nextLessonId, Title = "Second", SortOrder = 2, Status = ContentStatus.Published }
                    ] }]
            });
            db.Questions.Add(new Question
            {
                Id = questionId, LessonId = firstLessonId, Type = QuestionType.MultipleChoice,
                Prompt = "Choose hello", Explanation = "Hello is the greeting.",
                Options = [new QuestionOption { Id = optionId, Content = "hello", IsCorrect = true, SortOrder = 1 }]
            });
            await db.SaveChangesAsync();
        }

        var locked = await learner.GetAsync($"/api/v1/learning-journey/lessons/{nextLessonId}");
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
        var first = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{firstLessonId}");
        Assert.Equal("intro", first!.CurrentStepId);
        var intro = await learner.PutAsJsonAsync($"/api/v1/learning-journey/lessons/{firstLessonId}/steps/intro",
            new SaveLessonJourneyStepRequest { CompleteIntroduction = true, ExpectedRevision = 0 });
        Assert.Equal(HttpStatusCode.OK, intro.StatusCode);
        using var incorrect = await learner.PostAsJsonAsync("/api/v1/quiz-attempts", new SubmitQuizRequest
        {
            LessonId = firstLessonId, ClientAttemptId = Guid.NewGuid(), Answers = []
        });
        Assert.Equal(HttpStatusCode.OK, incorrect.StatusCode);
        var needsPractice = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{firstLessonId}");
        Assert.Contains(needsPractice!.WeakPoints, item => item.StepType == "quiz" && item.Prompt == "Choose hello" && item.IncorrectAttempts == 1);
        var request = new SubmitQuizRequest
        {
            LessonId = firstLessonId, ClientAttemptId = Guid.NewGuid(),
            Answers = [new QuizAnswerSubmission { QuestionId = questionId, SelectedOptionId = optionId }]
        };
        var submitted = await learner.PostAsJsonAsync("/api/v1/quiz-attempts", request);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var firstResult = await submitted.Content.ReadFromJsonAsync<QuizResultResponse>();
        Assert.True(firstResult!.Passed);
        var retry = await learner.PostAsJsonAsync("/api/v1/quiz-attempts", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(firstResult.AttemptId, (await retry.Content.ReadFromJsonAsync<QuizResultResponse>())!.AttemptId);
        var completed = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{firstLessonId}");
        Assert.Equal("Completed", completed!.Status);
        Assert.Empty(completed.WeakPoints);
        var next = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{nextLessonId}");
        Assert.Equal("intro", next!.CurrentStepId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.QuizAttempts.CountAsync(item => item.LessonId == firstLessonId));
            Assert.Equal(2, await db.LearningEvidences.CountAsync(item => item.LessonId == firstLessonId));
        }
    }

    [Fact]
    public async Task RequiredStepsControlCompletion_AndResumeMovesToNextStep()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        var courseId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
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
                Id = courseId, LanguageId = language.Id, Title = "Guided lesson", Level = "A1", Domain = "general",
                Status = ContentStatus.Published,
                MaterialJson = JsonSerializer.Serialize(new { units = new[] { new { lessons = new[] { new
                {
                    id = lessonId, summary = "Learn and check", flashcards = new[] { new { id = cardId, front = "hello", back = "xin chào",
                        example = "I say hello.", exampleTranslation = "Tôi nói xin chào.", clozeSentence = "I say ___.", acceptedAnswers = new[] { "hello" } } },
                    lessonSteps = new object[]
                    {
                        new { id = "intro", type = "introduction", title = "Introduction", order = 0, required = true,
                            activityRefs = Array.Empty<string>(), completionPolicy = new { kind = "viewed" } },
                        new { id = "words", type = "vocabulary", title = "Words", order = 1, required = true,
                            activityRefs = Array.Empty<string>(), completionPolicy = new { kind = "session_completed" } }
                    }
                } } } } }),
                Units = [new Unit { Id = unitId, Title = "Unit", SortOrder = 1,
                    Lessons = [new Lesson { Id = lessonId, Title = "Lesson", SortOrder = 1, Status = ContentStatus.Published }] }]
            });
            await db.SaveChangesAsync();
        }
        var initial = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{lessonId}");
        Assert.Equal("intro", initial!.CurrentStepId);
        Assert.Equal("Locked", initial.Steps[1].Status);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var edited = await db.Courses.SingleAsync(item => item.Id == courseId);
            edited.MaterialJson = edited.MaterialJson!.Replace("Learn and check", "Updated objective");
            await db.SaveChangesAsync();
        }
        using var outdated = await learner.PutAsJsonAsync($"/api/v1/learning-journey/lessons/{lessonId}/steps/intro",
            new SaveLessonJourneyStepRequest { ContentVersion = initial.ContentVersion, ExpectedRevision = 0 });
        Assert.True(outdated.StatusCode == HttpStatusCode.Conflict, await outdated.Content.ReadAsStringAsync());
        var revised = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{lessonId}");
        Assert.NotEqual(initial.ContentVersion, revised!.ContentVersion);
        var startedLesson = await learner.PutAsJsonAsync($"/api/v1/learning-journey/lessons/{lessonId}/steps/intro",
            new SaveLessonJourneyStepRequest { ContentVersion = revised.ContentVersion, ExpectedRevision = 0 });
        Assert.Equal(HttpStatusCode.OK, startedLesson.StatusCode);
        var startedState = (await startedLesson.Content.ReadFromJsonAsync<LessonJourneyResponse>())!;
        Assert.Equal(1, startedState.Steps.Single(step => step.Id == "intro").Revision);
        var firstResume = await learner.GetFromJsonAsync<ResumeLearningResponse>("/api/v1/progress/resume");
        Assert.Equal("intro", firstResume!.StepId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            Assert.True(await db.CourseEnrollments.AnyAsync(item => item.UserId == user.Id && item.CourseId == courseId));
        }
        var visit = await learner.PutAsJsonAsync($"/api/v1/learning-journey/lessons/{lessonId}/steps/intro",
            new SaveLessonJourneyStepRequest { CompleteIntroduction = true, ExpectedRevision = 1 });
        Assert.Equal(HttpStatusCode.OK, visit.StatusCode);
        var stale = await learner.PutAsJsonAsync($"/api/v1/learning-journey/lessons/{lessonId}/steps/intro",
            new SaveLessonJourneyStepRequest { Cursor = 2, ExpectedRevision = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var after = await visit.Content.ReadFromJsonAsync<LessonJourneyResponse>();
        Assert.Equal("words", after!.CurrentStepId);
        Assert.Equal("Available", after.Steps[1].Status);
        Assert.Equal("InProgress", after.Status);
        Assert.False(after.ContentChangedSinceStart);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.LearningSessions.CountAsync(item => item.LessonId == lessonId));
            Assert.Equal(1, await db.LearningEvidences.CountAsync(item => item.LessonId == lessonId && item.StepId == "intro"));
        }
        var resume = await learner.GetFromJsonAsync<ResumeLearningResponse>("/api/v1/progress/resume");
        Assert.Equal("words", resume!.StepId);
        var checkpoint = await learner.PutAsJsonAsync($"/api/v1/learning-journey/lessons/{lessonId}/steps/words",
            new SaveLessonJourneyStepRequest { Cursor = 1, ActivityId = cardId.ToString(), DraftJson = "{\"note\":\"resume me\"}", ExpectedRevision = 0 });
        Assert.Equal(HttpStatusCode.OK, checkpoint.StatusCode);
        resume = await learner.GetFromJsonAsync<ResumeLearningResponse>("/api/v1/progress/resume");
        Assert.Equal("words", resume!.StepId);
        Assert.Equal(1, resume.Cursor);
        Assert.Equal(cardId.ToString(), resume.ActivityId);
        var checkpointed = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{lessonId}");
        Assert.Equal(1, checkpointed!.Steps.Single(step => step.Id == "words").Cursor);
        Assert.Equal("{\"note\":\"resume me\"}", checkpointed.Steps.Single(step => step.Id == "words").DraftJson);
        var roadmap = await learner.GetFromJsonAsync<LearningRoadmapResponse>("/api/v1/progress/roadmap");
        Assert.Contains(roadmap!.Courses.SelectMany(course => course.Units).SelectMany(unit => unit.Lessons),
            lesson => lesson.Id == lessonId && lesson.Status == "InProgress");
        var started = await learner.PostAsJsonAsync("/api/v1/vocabulary-sessions",
            new StartVocabularySessionRequest { LessonId = lessonId });
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var session = (await started.Content.ReadFromJsonAsync<VocabularySessionResponse>())!;
        while (!session.Completed)
        {
            if (session.CanCompleteAfterRoundOne)
            {
                var skipped = await learner.PostAsync($"/api/v1/vocabulary-sessions/{session.Id}/complete-perfect-round", null);
                Assert.Equal(HttpStatusCode.OK, skipped.StatusCode);
                session = (await skipped.Content.ReadFromJsonAsync<VocabularySessionResponse>())!;
                break;
            }
            var step = session.Step!;
            var answered = await learner.PostAsJsonAsync($"/api/v1/vocabulary-sessions/{session.Id}/answers",
                new VocabularyPracticeAnswerRequest { StepId = step.Id,
                    Answer = step.Kind == "listenMeaning" ? "xin chào" : "hello",
                    Matches = step.Kind == "matching" ? new Dictionary<string, string> { ["hello"] = "xin chào" } : null });
            Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
            session = (await answered.Content.ReadFromJsonAsync<VocabularyAnswerResponse>())!.Session;
        }
        Assert.True(session.Completed);
        var finished = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{lessonId}");
        Assert.Equal("Completed", finished!.Status);
        Assert.Equal(2, finished.CompletedRequiredSteps);
        await learner.PostAsync($"/api/v1/learning-journey/lessons/{lessonId}/refresh", null);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.LearningEvidences.CountAsync(item => item.LessonId == lessonId));
            Assert.NotNull((await db.LearningSessions.SingleAsync(item => item.LessonId == lessonId)).CompletedAt);
        }
    }
}
