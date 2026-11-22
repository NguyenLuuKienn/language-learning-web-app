using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Admin;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.IntegrationTests.Infrastructure;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public class AdminApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_WhenLearner_ReturnsForbidden()
    {
        var learnerClient = await _factory.CreateAuthenticatedClientAsync(
            "learner@languagelearning.app", "Learner@123456");

        var response = await learnerClient.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WhenAdmin_ReturnsUserList()
    {
        var adminClient = await _factory.CreateAuthenticatedClientAsync(
            "admin@languagelearning.app", "Admin@123456");

        var response = await adminClient.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Email == "admin@languagelearning.app");
    }

    [Fact]
    public async Task Admin_CanOpenAndEnrollInAnotherLevelWithoutPlacement()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        using var learner = await _factory.CreateAuthenticatedClientAsync("learner@languagelearning.app", "Learner@123456");
        var languages = await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var english = Assert.Single(languages!, item => item.Code == "en");
        using var created = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Admin cross level preview", Level = "A2", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var course = await created.Content.ReadFromJsonAsync<CourseResponse>();
        Assert.NotNull(course);

        using var adminDetail = await admin.GetAsync($"/api/v1/courses/{course.Id}");
        using var adminEnrollment = await admin.PostAsync($"/api/v1/courses/{course.Id}/enroll", null);
        using var learnerDetail = await learner.GetAsync($"/api/v1/courses/{course.Id}");
        using var learnerEnrollment = await learner.PostAsync($"/api/v1/courses/{course.Id}/enroll", null);

        Assert.Equal(HttpStatusCode.OK, adminDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminEnrollment.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, learnerDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, learnerEnrollment.StatusCode);
    }

    [Fact]
    public async Task UserAdministration_RejectsMissingStatusAndInvalidRole()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var userId = Guid.NewGuid();
        using var missingStatus = await admin.PatchAsJsonAsync($"/api/v1/admin/users/{userId}/status", new { });
        Assert.Equal(HttpStatusCode.BadRequest, missingStatus.StatusCode);
        using var invalidRole = await admin.PatchAsJsonAsync($"/api/v1/admin/users/{userId}/role", new { role = 999 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);
    }

    [Fact]
    public async Task LegacyBulkCurriculumImport_IsUnavailable()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        using var response = await admin.PostAsJsonAsync("/api/v1/admin/curriculum/import", new { courses = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LessonMaterialEditor_WorksForManuallyCreatedLessons()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var english = Assert.Single(languages!, item => item.Code == "en");
        using var createdCourse = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Editable course", Level = "A1", Domain = "general"
        });
        var course = await createdCourse.Content.ReadFromJsonAsync<CourseResponse>();
        Assert.NotNull(course);
        using var createdUnit = await admin.PostAsJsonAsync($"/api/v1/admin/courses/{course.Id}/units", new CreateUnitRequest { Title = "Unit one", SortOrder = 1 });
        var unit = await createdUnit.Content.ReadFromJsonAsync<UnitResponse>();
        Assert.NotNull(unit);
        using var createdLesson = await admin.PostAsJsonAsync($"/api/v1/admin/courses/units/{unit.Id}/lessons", new CreateLessonRequest { Title = "Lesson one", SortOrder = 1, LessonType = "Vocabulary" });
        var lesson = await createdLesson.Content.ReadFromJsonAsync<LessonDetailResponse>();
        Assert.NotNull(lesson);

        var draft = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/courses/{course.Id}/material");
        var lessonNode = System.Text.Json.Nodes.JsonNode.Parse(draft.GetProperty("units")[0].GetProperty("lessons")[0].GetRawText())!;
        lessonNode["flashcards"] = new System.Text.Json.Nodes.JsonArray(new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = Guid.NewGuid().ToString(), ["front"] = "hello", ["back"] = "xin chao"
        }, new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = Guid.NewGuid().ToString(), ["front"] = "goodbye", ["back"] = "tam biet"
        });
        using var saved = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        var detail = await admin.GetFromJsonAsync<LessonDetailResponse>($"/api/v1/lessons/{lesson.Id}");
        Assert.Contains(detail!.Vocabularies, item => item.Term == "hello");
        var originalHelloId = detail.Vocabularies.Single(item => item.Term == "hello").Id;
        var originalGoodbyeId = detail.Vocabularies.Single(item => item.Term == "goodbye").Id;
        lessonNode["flashcards"]![0]!["back"] = "xin chào";
        lessonNode["flashcards"]![0]!["partOfSpeech"] = "interjection";
        lessonNode["flashcards"]![0]!["audioUrl"] = "https://example.com/hello.mp3";
        using var updatedCard = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.NoContent, updatedCard.StatusCode);
        var updatedDetail = await admin.GetFromJsonAsync<LessonDetailResponse>($"/api/v1/lessons/{lesson.Id}");
        Assert.Equal(originalHelloId, updatedDetail!.Vocabularies.Single(item => item.Term == "hello").Id);
        Assert.Equal(originalGoodbyeId, updatedDetail.Vocabularies.Single(item => item.Term == "goodbye").Id);
        Assert.Equal("xin chào", updatedDetail.Vocabularies.Single(item => item.Term == "hello").Meaning);
        Assert.Equal("interjection", updatedDetail.Vocabularies.Single(item => item.Term == "hello").PartOfSpeech);
        Assert.Equal("https://example.com/hello.mp3", updatedDetail.Vocabularies.Single(item => item.Term == "hello").AudioUrl);
        var reorderedCards = lessonNode["flashcards"]!.AsArray();
        var firstCard = reorderedCards[0]!.DeepClone();
        var secondCard = reorderedCards[1]!.DeepClone();
        reorderedCards[0] = secondCard;
        reorderedCards[1] = firstCard;
        using var reordered = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.NoContent, reordered.StatusCode);
        var reorderedDetail = await admin.GetFromJsonAsync<LessonDetailResponse>($"/api/v1/lessons/{lesson.Id}");
        Assert.Equal(originalHelloId, reorderedDetail!.Vocabularies.Single(item => item.Term == "hello").Id);
        Assert.Equal(originalGoodbyeId, reorderedDetail.Vocabularies.Single(item => item.Term == "goodbye").Id);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var learner = await db.Users.SingleAsync(item => item.Email == "learner@languagelearning.app");
            db.LearningSessions.Add(new LearningSession
            {
                Id = Guid.NewGuid(), UserId = learner.Id, LessonId = lesson.Id, ContentVersion = "prior-version"
            });
            await db.SaveChangesAsync();
        }
        lessonNode["lessonSteps"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"id\":\"intro\",\"type\":\"introduction\"},{\"id\":\"words\",\"type\":\"vocabulary\"}]");
        using var changedSteps = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.Conflict, changedSteps.StatusCode);
        lessonNode.AsObject().Remove("lessonSteps");
        lessonNode["flashcards"]![1]!["front"] = "changed after learning";
        using var changedAnswer = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.Conflict, changedAnswer.StatusCode);
        lessonNode["flashcards"]![1]!["front"] = "hello";
        lessonNode["summary"] = "Updated objective";
        using var stableSteps = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.NoContent, stableSteps.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var learner = await db.Users.SingleAsync(item => item.Email == "learner@languagelearning.app");
            db.LearningSessions.RemoveRange(db.LearningSessions.Where(item => item.LessonId == lesson.Id));
            db.VocabularySessions.Add(new VocabularySession
            {
                Id = Guid.NewGuid(), UserId = learner.Id, LessonId = lesson.Id
            });
            await db.SaveChangesAsync();
        }
        lessonNode["flashcards"]![1]!["back"] = "changed meaning";
        using var changedWithVocabularySession = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", lessonNode);
        Assert.Equal(HttpStatusCode.Conflict, changedWithVocabularySession.StatusCode);
    }

    [Fact]
    public async Task SrsEvaluationReadiness_IsAggregatedForAdminOnly()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var report = await admin.GetFromJsonAsync<SrsEvaluationReadinessResponse>("/api/v1/admin/srs/evaluation-readiness");
        Assert.NotNull(report);
        Assert.True(report.TotalReviews >= report.DueReviewsWithInterval);
        Assert.True(report.DueReviewsWithInterval >= report.DueReviewsRecalled);
        using var learner = await _factory.CreateAuthenticatedClientAsync("learner@languagelearning.app", "Learner@123456");
        using var forbidden = await learner.GetAsync("/api/v1/admin/srs/evaluation-readiness");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task LessonStepRemap_PreservesExistingCheckpointAndRejectsStaleVersion()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var english = Assert.Single((await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages"))!, item => item.Code == "en");
        using var createdCourse = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Remap course", Level = "A1", Domain = "general"
        });
        var course = (await createdCourse.Content.ReadFromJsonAsync<CourseResponse>())!;
        using var createdUnit = await admin.PostAsJsonAsync($"/api/v1/admin/courses/{course.Id}/units",
            new CreateUnitRequest { Title = "Unit", SortOrder = 1 });
        var unit = (await createdUnit.Content.ReadFromJsonAsync<UnitResponse>())!;
        using var createdLesson = await admin.PostAsJsonAsync($"/api/v1/admin/courses/units/{unit.Id}/lessons",
            new CreateLessonRequest { Title = "Lesson", SortOrder = 1, LessonType = "Vocabulary" });
        var lesson = (await createdLesson.Content.ReadFromJsonAsync<LessonDetailResponse>())!;
        var draft = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/courses/{course.Id}/material");
        var node = System.Text.Json.Nodes.JsonNode.Parse(draft.GetProperty("units")[0].GetProperty("lessons")[0].GetRawText())!;
        node["flashcards"] = new System.Text.Json.Nodes.JsonArray(new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = Guid.NewGuid().ToString(), ["front"] = "hello", ["back"] = "xin chao"
        });
        node["lessonSteps"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"id\":\"intro-old\",\"type\":\"introduction\",\"title\":\"Intro\",\"order\":0,\"required\":true,\"activityRefs\":[],\"completionPolicy\":{\"kind\":\"viewed\"}},{\"id\":\"words-old\",\"type\":\"vocabulary\",\"title\":\"Words\",\"order\":1,\"required\":true,\"activityRefs\":[],\"completionPolicy\":{\"kind\":\"session_completed\"}}]");
        using var saved = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/material", node);
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        var journey = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/learning-journey/lessons/{lesson.Id}");
        var version = journey.GetProperty("contentVersion").GetString()!;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var learner = await db.Users.SingleAsync(item => item.Email == "learner@languagelearning.app");
            db.LearningSessions.Add(new LearningSession { Id = Guid.NewGuid(), UserId = learner.Id, LessonId = lesson.Id, ContentVersion = version });
            db.LearningEvidences.Add(new LearningEvidence { Id = Guid.NewGuid(), UserId = learner.Id, LessonId = lesson.Id,
                StepId = "intro-old", StepType = "introduction", Outcome = "Completed", ContentVersion = version });
            db.LessonStepProgresses.Add(new LessonStepProgress { Id = Guid.NewGuid(), UserId = learner.Id, LessonId = lesson.Id,
                StepId = "words-old", Status = "InProgress", Revision = 2 });
            db.LessonProgresses.Add(new LessonProgress { Id = Guid.NewGuid(), UserId = learner.Id, LessonId = lesson.Id,
                Status = "InProgress", CurrentStepId = "words-old" });
            await db.SaveChangesAsync();
        }
        var url = $"/api/v1/admin/courses/{course.Id}/lessons/{lesson.Id}/remap-steps";
        var request = new RemapLessonStepsRequest { ExpectedContentVersion = version,
            StepIds = new() { ["intro-old"] = "intro-new", ["words-old"] = "words-new" } };
        using var remapped = await admin.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.NoContent, remapped.StatusCode);
        using var stale = await admin.PostAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("words-new", (await verify.LessonProgresses.SingleAsync(item => item.LessonId == lesson.Id)).CurrentStepId);
        Assert.Equal("words-new", (await verify.LessonStepProgresses.SingleAsync(item => item.LessonId == lesson.Id)).StepId);
        Assert.Equal("intro-new", (await verify.LearningEvidences.SingleAsync(item => item.LessonId == lesson.Id)).StepId);
        var newVersion = (await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/learning-journey/lessons/{lesson.Id}"))
            .GetProperty("contentVersion").GetString();
        Assert.Equal(newVersion, (await verify.LearningSessions.SingleAsync(item => item.LessonId == lesson.Id)).ContentVersion);
    }

    [Fact]
    public async Task LanguageLifecycle_CreateUpdateDelete_ExecutesSuccessfully()
    {
        var adminClient = await _factory.CreateAuthenticatedClientAsync(
            "admin@languagelearning.app", "Admin@123456");

        var uniqueCode = $"t{Guid.NewGuid():N}"[..6];
        var createRequest = new CreateLanguageRequest
        {
            Code = uniqueCode,
            Name = "Test French",
            NativeName = "Français",
            FlagEmoji = "🇫🇷",
            IsActive = true
        };

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/admin/languages", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<LanguageResponse>();
        Assert.NotNull(created);
        Assert.Equal("Test French", created.Name);

        var updateRequest = new UpdateLanguageRequest
        {
            Name = "Updated French",
            NativeName = "Français Modern",
            FlagEmoji = "🇫🇷",
            IsActive = true
        };

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/v1/admin/languages/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<LanguageResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated French", updated.Name);

        var deleteResponse = await adminClient.DeleteAsync($"/api/v1/admin/languages/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Course_RejectsBlankOrOverlongTitle()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var english = Assert.Single(languages!, item => item.Code == "en");
        using var blank = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "   ", Level = "A1", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        using var longTitle = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = new string('X', 201), Level = "A1", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.BadRequest, longTitle.StatusCode);
        using var unsupportedDomain = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Valid title", Level = "A1", Domain = "unrestricted"
        });
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedDomain.StatusCode);
        using var mismatchedLevel = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Valid title", Level = "N5", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatchedLevel.StatusCode);

        using var created = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Valid title", Level = "A1", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var course = await created.Content.ReadFromJsonAsync<CourseResponse>();
        Assert.NotNull(course);
        using var update = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}", new UpdateCourseRequest
        {
            Title = "   ", Level = "A1", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        using var blankUnit = await admin.PostAsJsonAsync($"/api/v1/admin/courses/{course.Id}/units", new CreateUnitRequest
        {
            Title = "   "
        });
        Assert.Equal(HttpStatusCode.BadRequest, blankUnit.StatusCode);
        using var missingLesson = await admin.PostAsJsonAsync("/api/v1/vocabularies", new CreateVocabularyRequest
        {
            LanguageId = english.Id, Term = "hello", Meaning = "xin chào"
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingLesson.StatusCode);
    }

    [Fact]
    public async Task PlacementTests_CanBeAuthoredBeforeCourseMaterial()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var english = Assert.Single(languages!, item => item.Code == "en");
        using var created = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id, Title = "Placement before material", Level = "A1", Domain = "general"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var course = await created.Content.ReadFromJsonAsync<CourseResponse>();
        Assert.NotNull(course);

        var tests = new[] { new { level = "A1", questions = new object[]
        {
            new { section = "listening", prompt = "Listen", audioScript = "Hello", options = new[] { "Hello", "Goodbye" }, correctIndex = 0, points = 25 },
            new { section = "speaking", prompt = "Speak", expectedResponse = "Say hello", rubric = "Pronunciation and fluency", points = 25 },
            new { section = "reading", prompt = "Read", readingPassage = "Hello", options = new[] { "Hello", "Goodbye" }, correctIndex = 0, points = 25 },
            new { section = "writing", prompt = "Write", expectedResponse = "Write hello", rubric = "Accuracy and coherence", points = 25 }
        } } };
        using var updated = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/placement-tests", tests);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var saved = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/admin/courses/{course.Id}/placement-tests");
        Assert.Equal(4, saved[0].GetProperty("questions").GetArrayLength());
        var detail = await admin.GetFromJsonAsync<CourseDetailResponse>($"/api/v1/courses/{course.Id}");
        Assert.NotNull(detail);
        Assert.False(detail.HasMaterial);
    }

    [Fact]
    public async Task CourseMaterial_RequiresExistingCourseAndPersistsAllSections()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var english = Assert.Single(languages!, item => item.Code == "en");
        var create = await admin.PostAsJsonAsync("/api/v1/admin/courses", new CreateCourseRequest
        {
            LanguageId = english.Id,
            Title = "Material integration test",
            Level = "A1",
            Domain = "general"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var course = await create.Content.ReadFromJsonAsync<CourseResponse>();
        Assert.NotNull(course);

        var unitId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var material = new
        {
            id = course.Id,
            languageCode = "en",
            level = "A1",
            domain = "general",
            title = course.Title,
            units = new[] { new
            {
                id = unitId,
                title = "Unit one",
                lessons = new[] { new
                {
                    id = lessonId,
                    title = "Lesson one",
                    flashcards = new[] { new { front = "hello", back = "xin chao", phonetic = "/həˈloʊ/", partOfSpeech = "interjection", meanings = new[] { "xin chao", "lời chào" }, example = "Hello, Anna!" } },
                    listeningExercises = new[] { new { title = "Listen" } },
                    speakingExercises = new[] { new { title = "Speak" } },
                    readingArticles = new[] { new { title = "Read", content = new[] { "Hello, world." }, vocabularies = Array.Empty<object>(), quiz = new { question = "Meaning?", options = new[] { "Hello", "Bye" }, correctIndex = 0 } } },
                    writingPrompts = new[] { new { title = "Write", scenario = "Introduce yourself", instruction = "Write a greeting" } },
                    grammarTopics = new[] { new { title = "Grammar" } },
                    quizQuestions = new[] { new { question = "Choose hello", options = new[] { "hello", "bye" }, correctIndex = 0 } }
                } }
            } },
            decks = new[] { new { title = "Course cards", cards = new[] { new { front = "hello", back = "xin chao", phonetic = "/həˈloʊ/", partOfSpeech = "interjection", meanings = new[] { "xin chao", "lời chào" }, example = "Hello, Anna!" } } } },
            srs = new { algorithm = "srs-v1", dailyGoal = 10 },
            placementTests = new[] { new { level = "A1", questions = new object[]
            {
                new { section = "listening", prompt = "Listen", audioScript = "Hello", options = new[] { "a", "b" }, correctIndex = 0, points = 25 },
                new { section = "speaking", prompt = "Speak", expectedResponse = "hello", rubric = "Pronunciation and fluency", points = 25 },
                new { section = "reading", prompt = "Read", readingPassage = "Hello", options = new[] { "a", "b" }, correctIndex = 0, points = 25 },
                new { section = "writing", prompt = "Write", expectedResponse = "hello", rubric = "Accuracy and coherence", points = 25 }
            } } }
        };

        var invalidMaterial = System.Text.Json.JsonSerializer.SerializeToNode(material)!;
        invalidMaterial["units"]![0]!["lessons"]![0]!["writingPrompts"]![0]!["instruction"] = " ";
        using var rejected = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/material", invalidMaterial);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var invalidMeanings = System.Text.Json.JsonSerializer.SerializeToNode(material)!;
        invalidMeanings["units"]![0]!["lessons"]![0]!["flashcards"]![0]!["meanings"] = new System.Text.Json.Nodes.JsonArray();
        using var rejectedMeanings = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/material", invalidMeanings);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedMeanings.StatusCode);

        var imported = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/material", material);
        Assert.Equal(HttpStatusCode.NoContent, imported.StatusCode);

        var saved = await admin.GetAsync($"/api/v1/courses/{course.Id}/material");
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var detail = await admin.GetFromJsonAsync<CourseDetailResponse>($"/api/v1/courses/{course.Id}");
        Assert.NotNull(detail);
        Assert.Single(detail.Units);
        Assert.Single(detail.Units[0].Lessons);
        var vocabulary = await admin.GetFromJsonAsync<List<VocabularyResponse>>($"/api/v1/lessons/{lessonId}/vocabulary");
        var word = Assert.Single(vocabulary!);
        Assert.Equal(2, word.Meanings.Count);
        Assert.Equal("/həˈloʊ/", word.Phonetic);
        Assert.Equal("interjection", word.PartOfSpeech);

        var editedLesson = System.Text.Json.JsonSerializer.SerializeToNode(material)!["units"]![0]!["lessons"]![0]!;
        editedLesson["flashcards"]![0]!["front"] = "good morning";
        editedLesson["quizQuestions"]![0]!["question"] = "Choose good morning";
        editedLesson["writingPrompts"]![0]!["instruction"] = "Write a polite greeting";
        var invalidEdit = editedLesson.DeepClone();
        invalidEdit["writingPrompts"]![0]!["instruction"] = " ";
        using var rejectedEdit = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lessonId}/material", invalidEdit);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedEdit.StatusCode);
        var invalidListening = editedLesson.DeepClone();
        invalidListening["listeningExercises"]![0]!["audioScript"] = " ";
        using var rejectedListening = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lessonId}/material", invalidListening);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedListening.StatusCode);
        using var updatedLesson = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/lessons/{lessonId}/material", editedLesson);
        Assert.Equal(HttpStatusCode.NoContent, updatedLesson.StatusCode);
        var updatedMaterial = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/courses/{course.Id}/material");
        var savedLesson = updatedMaterial.GetProperty("units")[0].GetProperty("lessons")[0];
        Assert.Equal("Write a polite greeting", savedLesson.GetProperty("writingPrompts")[0].GetProperty("instruction").GetString());
        var lessonDetail = await admin.GetFromJsonAsync<LessonDetailResponse>($"/api/v1/lessons/{lessonId}");
        Assert.Contains(lessonDetail!.Vocabularies, item => item.Term == "good morning");
        var quiz = await admin.GetFromJsonAsync<List<LanguageLearning.Application.DTOs.Quiz.QuizQuestionResponse>>($"/api/v1/lessons/{lessonId}/quiz");
        var onlyQuestion = Assert.Single(quiz!);
        Assert.Equal("Choose good morning", onlyQuestion.Prompt);

        var tests = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/admin/courses/{course.Id}/placement-tests");
        Assert.Equal(1, tests.GetArrayLength());
        var updatedTests = new[] { new { level = "A1", questions = new object[]
        {
            new { section = "listening", prompt = "New listening", audioScript = "Hello", options = new[] { "a", "b" }, correctIndex = 0, points = 25 },
            new { section = "speaking", prompt = "New speaking", expectedResponse = "hello", rubric = "Pronunciation and fluency", points = 25 },
            new { section = "reading", prompt = "New reading", readingPassage = "Hello", options = new[] { "a", "b" }, correctIndex = 0, points = 25 },
            new { section = "writing", prompt = "New writing", expectedResponse = "hello", rubric = "Accuracy and coherence", points = 25 }
        } } };
        var updateExam = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/placement-tests", updatedTests);
        Assert.Equal(HttpStatusCode.NoContent, updateExam.StatusCode);
        var changed = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/admin/courses/{course.Id}/placement-tests");
        Assert.Equal("New listening", changed[0].GetProperty("questions")[0].GetProperty("prompt").GetString());
        using var variants = await admin.PutAsJsonAsync($"/api/v1/admin/courses/{course.Id}/placement-tests", new[] { updatedTests[0], updatedTests[0] });
        Assert.Equal(HttpStatusCode.NoContent, variants.StatusCode);
        var exam = await admin.GetFromJsonAsync<LanguageLearning.Application.DTOs.Auth.PlacementExamDto>($"/api/v1/placement/courses/{course.Id}");
        Assert.Equal(4, exam!.Questions.Count);
    }
}
