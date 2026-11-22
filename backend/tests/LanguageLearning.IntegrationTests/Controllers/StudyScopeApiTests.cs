using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.Common;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class StudyScopeApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StudyScopeApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task A1Learner_CannotReadA2DeckSrsProgressVocabularyOrMedia()
    {
        using var learner = await _factory.CreateAuthenticatedClientAsync();
        Guid a1DeckId;
        Guid a2DeckId;
        Guid a2CardId;
        Guid a2LessonId;
        Guid a2CourseId;
        Guid a2VocabularyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            a1DeckId = Guid.NewGuid();
            a2DeckId = Guid.NewGuid();
            a2CardId = Guid.NewGuid();
            a2LessonId = Guid.NewGuid();
            a2CourseId = Guid.NewGuid();
            a2VocabularyId = Guid.NewGuid();
            db.Decks.AddRange(
                new Deck { Id = a1DeckId, OwnerId = user.Id, LanguageId = language.Id, Title = "A1", Tags = [new DeckTag { Id = Guid.NewGuid(), Tag = "course-level:A1" }], Cards = [new Card { Id = Guid.NewGuid(), Front = "hello", Back = "xin chào" }] },
                new Deck { Id = a2DeckId, OwnerId = user.Id, LanguageId = language.Id, Title = "A2", IsPublic = true, Tags = [new DeckTag { Id = Guid.NewGuid(), Tag = "course-level:A2" }], Cards = [new Card { Id = a2CardId, Front = "advanced", Back = "nâng cao" }] });
            db.Courses.Add(new Course { Id = a2CourseId, LanguageId = language.Id, Title = "A2", Level = "A2", Domain = "general", Status = ContentStatus.Published,
                Units = [new Unit { Id = Guid.NewGuid(), Title = "Unit", Lessons = [new Lesson { Id = a2LessonId, Title = "Lesson", Status = ContentStatus.Published,
                    Vocabularies = [new Vocabulary { Id = a2VocabularyId, LanguageId = language.Id, Term = "advanced", Meaning = "nâng cao", AudioUrl = "/api/v1/media/private.mp3" }] }] }] });
            await db.SaveChangesAsync();
        }

        var decks = await learner.GetFromJsonAsync<PagedResponse<DeckResponse>>("/api/v1/decks?page=1&pageSize=20");
        Assert.NotNull(decks);
        Assert.Single(decks.Items);
        Assert.Equal(a1DeckId, decks.Items[0].Id);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/decks/{a2DeckId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/decks/{a2DeckId}/cards")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/public/decks/{a2DeckId}/preview")).StatusCode);
        var due = await learner.GetFromJsonAsync<DueCardsSummaryResponse>("/api/v1/reviews/due");
        Assert.NotNull(due);
        Assert.Single(due.Cards);
        Assert.Equal(a1DeckId, due.Cards[0].DeckId);
        var overview = await learner.GetFromJsonAsync<ReviewOverviewResponse>("/api/v1/reviews/overview");
        Assert.NotNull(overview);
        Assert.Equal(1, overview.UnseenCount);
        Assert.Equal(0, overview.DueCount);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/reviews/due?deckId={a2DeckId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/progress/lessons/{a2LessonId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/progress/courses/{a2CourseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/vocabularies/{a2VocabularyId}")).StatusCode);
        var search = await learner.GetFromJsonAsync<List<VocabularyResponse>>("/api/v1/vocabularies?term=advanced");
        Assert.Empty(search!);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync("/api/v1/media/private.mp3")).StatusCode);
        using var guest = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/api/v1/media/private.mp3")).StatusCode);
    }
}
