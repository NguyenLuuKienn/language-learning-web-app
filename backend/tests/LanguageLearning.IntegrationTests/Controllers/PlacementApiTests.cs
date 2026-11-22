using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class PlacementApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlacementApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Placement_RequiresAudioAndReviewBeforeProofUnlocksCourseMaterial()
    {
        Guid courseId;
        Guid languageId;
        Guid lessonId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            languageId = language.Id;
            courseId = Guid.NewGuid();
            lessonId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var questions = new object[]
            {
                new { section = "listening", prompt = "Hear greeting", audioScript = "Hello", options = new[] { "Hello", "Goodbye" }, correctIndex = 0, points = 25 },
                new { section = "speaking", prompt = "Greet", expectedResponse = "hello world", minWords = 2, points = 25 },
                new { section = "reading", prompt = "Read greeting", readingPassage = "Hello", options = new[] { "Hello", "Goodbye" }, correctIndex = 0, points = 25 },
                new { section = "writing", prompt = "Write greeting", expectedResponse = "hello world", minWords = 2, points = 25 }
            };
            db.Courses.Add(new Course
            {
                Id = courseId, LanguageId = languageId, Title = "Placement security course", Level = "A2",
                Domain = "general", Status = ContentStatus.Published,
                MaterialJson = JsonSerializer.Serialize(new
                {
                    id = courseId, languageCode = "en", level = "A2", domain = "general",
                    placementTests = new[] { new { level = "A2", questions } }, units = Array.Empty<object>()
                }),
                Units = [new Unit
                {
                    Id = unitId, Title = "Unit", SortOrder = 1,
                    Lessons = [new Lesson { Id = lessonId, Title = "Lesson", SortOrder = 1, Status = ContentStatus.Published }]
                }]
            });
            await db.SaveChangesAsync();
        }

        using var client = await _factory.CreateAuthenticatedClientAsync();
        using var hidden = await client.GetAsync($"/api/v1/courses/{courseId}/material");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/courses/{courseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/lessons/{lessonId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/lessons/{lessonId}/quiz")).StatusCode);

        using var noProof = await client.PutAsJsonAsync("/api/v1/me/study-target",
            new StudyTargetDto { LanguageId = languageId, Level = "A2", Domain = "general" });
        Assert.Equal(HttpStatusCode.BadRequest, noProof.StatusCode);

        var exam = await client.GetFromJsonAsync<PlacementExamDto>($"/api/v1/placement/courses/{courseId}");
        Assert.NotNull(exam);
        Assert.Equal(4, exam.Questions.Count);
        var examJson = JsonSerializer.Serialize(exam);
        Assert.DoesNotContain("correctIndex", examJson);
        Assert.DoesNotContain("expectedResponse", examJson);

        using var failedSubmission = await client.PostAsJsonAsync($"/api/v1/placement/courses/{courseId}/submit", new PlacementSubmissionDto());
        Assert.Equal(HttpStatusCode.BadRequest, failedSubmission.StatusCode);

        var speakingId = Assert.Single(exam.Questions, item => item.Section == "speaking").Id;
        var recording = new byte[1200];
        new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }.CopyTo(recording, 0);
        using var form = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(recording);
        audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
        form.Add(audioContent, "file", "speaking.webm");
        using var audioUpload = await client.PostAsync($"/api/v1/placement/courses/{courseId}/audio/{speakingId}", form);
        Assert.Equal(HttpStatusCode.OK, audioUpload.StatusCode);
        using var uploaded = await audioUpload.Content.ReadFromJsonAsync<JsonDocument>();
        var fileName = uploaded!.RootElement.GetProperty("fileName").GetString();
        Assert.False(string.IsNullOrWhiteSpace(fileName));
        using var ownerAudio = await client.GetAsync($"/api/v1/media/{fileName}");
        Assert.Equal(HttpStatusCode.OK, ownerAudio.StatusCode);

        var answers = new PlacementSubmissionDto
        {
            SelectedOptions = exam.Questions.Where(item => item.Section is "listening" or "reading")
                .ToDictionary(item => item.Id, _ => 0),
            WrittenAnswers = exam.Questions.Where(item => item.Section is "speaking" or "writing")
                .ToDictionary(item => item.Id, _ => "hello world"),
            AudioFileNames = new Dictionary<string, string> { [speakingId] = fileName! }
        };
        using var submission = await client.PostAsJsonAsync($"/api/v1/placement/courses/{courseId}/submit", answers);
        Assert.Equal(HttpStatusCode.OK, submission.StatusCode);
        var result = await submission.Content.ReadFromJsonAsync<PlacementResultDto>();
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.False(result.Passed);
        Assert.Null(result.PlacementProof);

        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var pending = await admin.GetFromJsonAsync<List<PlacementReviewDto>>("/api/v1/placement/admin/pending");
        var reviewItem = Assert.Single(pending!, item => item.AttemptId == result.AttemptId);
        var audioUrl = Assert.Single(reviewItem.Questions, item => item.Section == "speaking").AudioUrl;
        Assert.NotNull(audioUrl);
        using var reviewerAudio = await admin.GetAsync(audioUrl);
        Assert.Equal(HttpStatusCode.OK, reviewerAudio.StatusCode);
        var review = new PlacementReviewRequestDto
        {
            Scores = reviewItem.Questions.Where(item => item.Section is "speaking" or "writing")
                .ToDictionary(item => item.Id, item => item.Points),
            Feedback = "Pronunciation and writing meet the rubric."
        };
        using var reviewed = await admin.PostAsJsonAsync($"/api/v1/placement/admin/attempts/{result.AttemptId}/review", review);
        Assert.Equal(HttpStatusCode.OK, reviewed.StatusCode);
        var graded = await client.GetFromJsonAsync<PlacementResultDto>($"/api/v1/placement/attempts/{result.AttemptId}");
        Assert.NotNull(graded);
        Assert.True(graded.Passed);
        Assert.False(string.IsNullOrWhiteSpace(graded.PlacementProof));

        using var tampered = await client.PutAsJsonAsync("/api/v1/me/study-target",
            new StudyTargetDto { LanguageId = languageId, Level = "A2", Domain = "general", PlacementProof = graded.PlacementProof + "x" });
        Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);

        using var unlocked = await client.PutAsJsonAsync("/api/v1/me/study-target",
            new StudyTargetDto { LanguageId = languageId, Level = "A2", Domain = "general", PlacementProof = graded.PlacementProof });
        Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
        using var material = await client.GetAsync($"/api/v1/courses/{courseId}/material");
        Assert.Equal(HttpStatusCode.OK, material.StatusCode);
        var materialJson = await material.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correctIndex", materialJson);
        Assert.DoesNotContain("expectedResponse", materialJson);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/courses/{courseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/lessons/{lessonId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/lessons/{lessonId}/quiz")).StatusCode);
    }
}
