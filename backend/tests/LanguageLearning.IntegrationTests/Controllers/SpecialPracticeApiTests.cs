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

public sealed class SpecialPracticeApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public SpecialPracticeApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task IeltsToeicAndInterview_SaveResumeScoreAndProtectAnswers()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var courseId = Guid.NewGuid();
        var ieltsId = Guid.NewGuid();
        var toeicId = Guid.NewGuid();
        var interviewId = Guid.NewGuid();
        var listeningId = Guid.NewGuid();
        var writingId = Guid.NewGuid();
        var toeicQuestionId = Guid.NewGuid();
        var interviewTurnId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            db.Courses.Add(new Course { Id = courseId, LanguageId = language.Id, Title = "Practice course",
                Level = "A1", Domain = "general", Status = ContentStatus.Published });
            await db.SaveChangesAsync();
        }

        var content = new
        {
            certificateExams = new object[]
            {
                new { id = ieltsId, title = "IELTS practice", certificate = "IELTS", level = "A1",
                    sections = new object[]
                    {
                        new { skill = "listening", questions = new[] { new { id = listeningId,
                            prompt = "Where?", audioScript = "At the library.", options = new[] { "Library", "School" },
                            correctIndex = 0, points = 2 } } },
                        new { skill = "writing", questions = new[] { new { id = writingId,
                            prompt = "Write a message.", rubric = "Assess task response and clarity.", points = 10 } } }
                    } },
                new { id = toeicId, title = "TOEIC practice", certificate = "TOEIC", level = "A1", durationMinutes = 1,
                    sections = new object[] { new { skill = "reading", questions = new[] { new { id = toeicQuestionId,
                        prompt = "When?", passage = "Open Monday.", options = new[] { "Monday", "Sunday" },
                        correctIndex = 0, points = 1 } } } } }
            },
            interviewScenarios = new[] { new { id = interviewId, title = "Job interview", level = "A1",
                profession = "general", rubric = "Assess relevant and clear responses.",
                turns = new[] { new { id = interviewTurnId, prompt = "Introduce yourself." } } } }
        };
        var update = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{courseId}/special-practice", content);
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var adminBase = $"/api/v1/admin/courses/{courseId}/special-practice";
        Assert.Equal(HttpStatusCode.Forbidden, (await learner.GetAsync($"{adminBase}/certificate")).StatusCode);
        var examList = await admin.GetFromJsonAsync<List<JsonElement>>($"{adminBase}/certificate");
        Assert.Equal(2, examList!.Count);
        var extraId = Guid.NewGuid();
        var extraQuestionId = Guid.NewGuid();
        var extra = new { id = extraId, title = "TOEIC extra", certificate = "TOEIC", level = "A1",
            sections = new[] { new { skill = "reading", questions = new[] { new { id = extraQuestionId,
                prompt = "Choose one.", passage = "Open daily.", options = new[] { "Daily", "Never" },
                correctIndex = 0, points = 1 } } } } };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"{adminBase}/certificate", extra)).StatusCode);
        var renamed = new { extra.id, title = "TOEIC revised", extra.certificate, extra.level, extra.sections };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{adminBase}/certificate/{extraId}", renamed)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{adminBase}/certificate/{extraId}")).StatusCode);
        Assert.Equal(2, (await admin.GetFromJsonAsync<List<JsonElement>>($"{adminBase}/certificate"))!.Count);
        var invalidBatch = new object[] { extra, new { id = Guid.NewGuid(), title = "Invalid", certificate = "TOEIC", level = "B2", sections = extra.sections } };
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"{adminBase}/certificate/import", invalidBatch)).StatusCode);
        Assert.Equal(2, (await admin.GetFromJsonAsync<List<JsonElement>>($"{adminBase}/certificate"))!.Count);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync($"{adminBase}/certificate/import", new[] { extra })).StatusCode);
        Assert.Equal(3, (await admin.GetFromJsonAsync<List<JsonElement>>($"{adminBase}/certificate"))!.Count);

        var catalog = await learner.GetFromJsonAsync<List<SpecialPracticeCatalogItem>>("/api/v1/special-practice/catalog");
        Assert.Contains(catalog!, item => item.Certificate == "IELTS" && item.ContentId == ieltsId);
        Assert.Contains(catalog!, item => item.Certificate == "TOEIC" && item.ContentId == toeicId);
        Assert.Contains(catalog!, item => item.Kind == "interview" && item.ContentId == interviewId);

        var started = await learner.PostAsJsonAsync("/api/v1/special-practice/attempts",
            new StartSpecialPracticeRequest(courseId, ieltsId, "certificate"));
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var attempt = (await started.Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{adminBase}/certificate/{ieltsId}")).StatusCode);
        var archivedCatalog = await learner.GetFromJsonAsync<List<SpecialPracticeCatalogItem>>("/api/v1/special-practice/catalog");
        Assert.Contains(archivedCatalog!, item => item.ContentId == ieltsId && item.Archived && item.ActiveAttemptId == attempt.Id);
        Assert.Equal(2, attempt.Questions.Count);
        Assert.DoesNotContain("correctIndex", await started.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rubric", await started.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.GetAsync($"/api/v1/special-practice/attempts/{attempt.Id}")).StatusCode);
        var resumed = await learner.PostAsJsonAsync("/api/v1/special-practice/attempts",
            new StartSpecialPracticeRequest(courseId, ieltsId, "certificate"));
        Assert.Equal(attempt.Id, (await resumed.Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!.Id);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await learner.PostAsync($"/api/v1/special-practice/attempts/{attempt.Id}/submit", null)).StatusCode);
        await learner.PutAsJsonAsync($"/api/v1/special-practice/attempts/{attempt.Id}/answers",
            new SaveSpecialPracticeAnswerRequest(listeningId, 0, null));
        await learner.PutAsJsonAsync($"/api/v1/special-practice/attempts/{attempt.Id}/answers",
            new SaveSpecialPracticeAnswerRequest(writingId, null, "I would like to meet tomorrow."));
        var submitted = await learner.PostAsync($"/api/v1/special-practice/attempts/{attempt.Id}/submit", null);
        var result = (await submitted.Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        Assert.Equal("PendingReview", result.Status);
        Assert.Equal(2, result.ObjectiveScore);
        Assert.Equal(2, result.ObjectiveMaxScore);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await learner.GetAsync($"/api/v1/special-practice/reviews/{attempt.Id}")).StatusCode);
        var pending = await admin.GetFromJsonAsync<List<SpecialPracticeReviewItem>>("/api/v1/special-practice/reviews/pending");
        Assert.Contains(pending!, item => item.AttemptId == attempt.Id);
        var review = await admin.GetFromJsonAsync<SpecialPracticeReviewDetail>($"/api/v1/special-practice/reviews/{attempt.Id}");
        Assert.Equal(2, review!.Questions.Count);
        var badGrade = await admin.PostAsJsonAsync($"/api/v1/special-practice/reviews/{attempt.Id}",
            new GradeSpecialPracticeRequest(new Dictionary<Guid, int> { [writingId] = 11 }, "Feedback"));
        Assert.Equal(HttpStatusCode.BadRequest, badGrade.StatusCode);
        var graded = await admin.PostAsJsonAsync($"/api/v1/special-practice/reviews/{attempt.Id}",
            new GradeSpecialPracticeRequest(new Dictionary<Guid, int> { [writingId] = 8 }, "Clear response."));
        Assert.Equal(HttpStatusCode.OK, graded.StatusCode);
        var reviewed = await learner.GetFromJsonAsync<SpecialPracticeAttemptResponse>(
            $"/api/v1/special-practice/attempts/{attempt.Id}");
        Assert.Equal("Reviewed", reviewed!.Status);
        Assert.Equal(8, reviewed.SubjectiveScore);
        Assert.Equal("Clear response.", reviewed.Feedback);

        var toeic = (await (await learner.PostAsJsonAsync("/api/v1/special-practice/attempts",
            new StartSpecialPracticeRequest(courseId, toeicId, "certificate")))
            .Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        await learner.PutAsJsonAsync($"/api/v1/special-practice/attempts/{toeic.Id}/answers",
            new SaveSpecialPracticeAnswerRequest(toeicQuestionId, 1, null));
        var toeicResult = (await (await learner.PostAsync($"/api/v1/special-practice/attempts/{toeic.Id}/submit", null))
            .Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        Assert.Equal("Scored", toeicResult.Status);
        Assert.Equal(0, toeicResult.ObjectiveScore);
        var timed = (await (await learner.PostAsJsonAsync("/api/v1/special-practice/attempts",
            new StartSpecialPracticeRequest(courseId, toeicId, "certificate")))
            .Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        Assert.NotNull(timed.DeadlineAt);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.SpecialPracticeAttempts.FirstAsync(item => item.Id == timed.Id);
            stored.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
            await db.SaveChangesAsync();
        }
        var expired = await learner.GetFromJsonAsync<SpecialPracticeAttemptResponse>(
            $"/api/v1/special-practice/attempts/{timed.Id}");
        Assert.Equal("Scored", expired!.Status);
        Assert.Equal(0, expired.ObjectiveScore);
        Assert.Equal(1, expired.ObjectiveMaxScore);

        var interview = (await (await learner.PostAsJsonAsync("/api/v1/special-practice/attempts",
            new StartSpecialPracticeRequest(courseId, interviewId, "interview")))
            .Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        using (var multipart = new MultipartFormDataContent())
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes("RIFF1234WAVE");
            var audio = new ByteArrayContent(bytes);
            audio.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            multipart.Add(audio, "file", "answer.wav");
            var uploaded = await learner.PostAsync($"/api/v1/special-practice/attempts/{interview.Id}/questions/{interviewTurnId}/audio", multipart);
            Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
            var withAudio = await uploaded.Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>();
            Assert.StartsWith("/api/v1/media/", withAudio!.Answers[interviewTurnId].AudioUrl);
            Assert.Equal(HttpStatusCode.OK, (await learner.GetAsync(withAudio.Answers[interviewTurnId].AudioUrl)).StatusCode);
        }
        await learner.PutAsJsonAsync($"/api/v1/special-practice/attempts/{interview.Id}/answers",
            new SaveSpecialPracticeAnswerRequest(interviewTurnId, null, "I am a student."));
        var interviewResult = (await (await learner.PostAsync($"/api/v1/special-practice/attempts/{interview.Id}/submit", null))
            .Content.ReadFromJsonAsync<SpecialPracticeAttemptResponse>())!;
        Assert.Equal("PendingReview", interviewResult.Status);
    }
}
