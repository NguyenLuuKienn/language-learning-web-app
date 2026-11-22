using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class SkillPracticeApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public SkillPracticeApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ListeningProgress_RequiresCorrectServerGrading_AndResumes()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        var courseId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var activityId = Guid.NewGuid().ToString();
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
                Id = courseId, LanguageId = language.Id, Title = "Practice", Level = "A1",
                Domain = "general", Status = ContentStatus.Published,
                MaterialJson = JsonSerializer.Serialize(new { units = new[] { new { lessons = new[] {
                    new { id = lessonId, listeningExercises = new[] { new { id = activityId, title = "Hear the greeting", options = new[] { "A", "B" }, correctIndex = 1 } },
                        lessonSteps = new object[] {
                            new { id = "intro", type = "introduction", title = "Introduction", order = 0, required = true, activityRefs = Array.Empty<string>(), completionPolicy = new { kind = "viewed" } },
                            new { id = "listen", type = "listening", title = "Listening", order = 1, required = true, activityRefs = new[] { activityId }, completionPolicy = new { kind = "all_correct" } }
                        } }
                } } } }),
                Units = [new Unit { Id = Guid.NewGuid(), Title = "Unit", SortOrder = 1,
                    Lessons = [new Lesson { Id = lessonId, Title = "Lesson", SortOrder = 1, Status = ContentStatus.Published }] }]
            });
            await db.SaveChangesAsync();
        }

        var route = "/api/v1/skill-practice/listening";
        var started = await learner.PostAsJsonAsync(route, new SkillPracticeRequest { CourseId = courseId, ActivityId = activityId });
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal("InProgress", (await started.Content.ReadFromJsonAsync<SkillPracticeResponse>())!.Status);

        var wrong = await learner.PostAsJsonAsync(route, new SkillPracticeRequest { CourseId = courseId, ActivityId = activityId, AnswerIndex = 0, ClientAttemptId = Guid.NewGuid() });
        Assert.Equal(false, (await wrong.Content.ReadFromJsonAsync<SkillPracticeResponse>())!.Correct);
        var needsPractice = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{lessonId}");
        Assert.Contains(needsPractice!.WeakPoints, item => item.StepType == "listening" && item.Prompt == "Hear the greeting");
        var correctRequest = new SkillPracticeRequest { CourseId = courseId, ActivityId = activityId, AnswerIndex = 1, ClientAttemptId = Guid.NewGuid() };
        var correct = await learner.PostAsJsonAsync(route, correctRequest);
        var scored = (await correct.Content.ReadFromJsonAsync<SkillPracticeResponse>())!;
        Assert.True(scored.Correct);
        Assert.Equal("Completed", scored.Status);
        Assert.Equal(2, scored.Attempts);
        var recovered = await learner.GetFromJsonAsync<LessonJourneyResponse>($"/api/v1/learning-journey/lessons/{lessonId}");
        Assert.Empty(recovered!.WeakPoints);
        var retried = await learner.PostAsJsonAsync(route, correctRequest);
        Assert.Equal(2, (await retried.Content.ReadFromJsonAsync<SkillPracticeResponse>())!.Attempts);
        var saved = await learner.GetFromJsonAsync<List<SkillPracticeResponse>>(route);
        Assert.Contains(saved!, item => item.ActivityId == activityId && item.Status == "Completed");
        var forged = await learner.PostAsJsonAsync(route, new SkillPracticeRequest { CourseId = courseId, ActivityId = Guid.NewGuid().ToString(), AnswerIndex = 1 });
        Assert.Equal(HttpStatusCode.NotFound, forged.StatusCode);
    }
}
