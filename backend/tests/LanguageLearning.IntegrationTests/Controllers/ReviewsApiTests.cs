using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public class ReviewsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReviewsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDueCards_WhenAuthenticated_ReturnsDueCardsSummary()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/reviews/due");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<DueCardsSummaryResponse>();
        Assert.NotNull(summary);
        Assert.NotNull(summary.Cards);
    }

    [Fact]
    public async Task SubmitReview_WithGoodRating_UpdatesSrsSchedule()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dueResponse = await client.GetAsync("/api/v1/reviews/due");
        var summary = await dueResponse.Content.ReadFromJsonAsync<DueCardsSummaryResponse>();

        if (summary != null && summary.Cards.Count > 0)
        {
            var cardToReview = summary.Cards[0];
            var request = new SubmitReviewRequest
            {
                ClientEventId = Guid.NewGuid(),
                CardId = cardToReview.Id,
                Rating = SrsRating.Good,
                ReviewedAt = DateTime.UtcNow
            };

            var response = await client.PostAsJsonAsync("/api/v1/reviews", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ReviewResultResponse>();
            Assert.NotNull(result);
            Assert.Equal(cardToReview.Id, result.CardId);
            Assert.True(result.NextIntervalDays >= 1);
        }
    }

    [Fact]
    public async Task DueAndNewQueues_FollowTheSameDeckSchedule()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var deckId = Guid.NewGuid();
        var dueId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var futureId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            db.Decks.Add(new Deck
            {
                Id = deckId, OwnerId = user.Id, LanguageId = language.Id, Title = "SRS queue test",
                Tags = [new DeckTag { Id = Guid.NewGuid(), Tag = "course-level:a1" }],
                Cards =
                [
                    new Card { Id = dueId, Front = "due-test", Back = "đến hạn" },
                    new Card { Id = newId, Front = "new-test", Back = "mới" },
                    new Card { Id = futureId, Front = "future-test", Back = "chưa đến hạn" }
                ]
            });
            db.CardSchedules.AddRange(
                new CardSchedule { Id = Guid.NewGuid(), UserId = user.Id, CardId = dueId,
                    DueAt = DateTime.UtcNow.AddMinutes(-1), StateJson = new SrsState().ToJson() },
                new CardSchedule { Id = Guid.NewGuid(), UserId = user.Id, CardId = futureId,
                    DueAt = DateTime.UtcNow.AddDays(2), StateJson = new SrsState { Repetitions = 1, IntervalDays = 2 }.ToJson() });
            await db.SaveChangesAsync();
        }

        var overview = await client.GetFromJsonAsync<ReviewOverviewResponse>($"/api/v1/reviews/overview?deckId={deckId}");
        Assert.Equal(3, overview!.TotalCount);
        Assert.Equal(1, overview.DueCount);
        Assert.Equal(1, overview.UnseenCount);
        Assert.Equal(2, overview.LearningCount);
        Assert.Equal(0, overview.ReviewCount);
        Assert.Equal(1, overview.DueForecast7Days[2]);
        var due = await client.GetFromJsonAsync<DueCardsSummaryResponse>($"/api/v1/reviews/due?deckId={deckId}&dueOnly=true");
        Assert.Equal([dueId], due!.Cards.Select(card => card.Id));
        var fresh = await client.GetFromJsonAsync<DueCardsSummaryResponse>($"/api/v1/reviews/due?deckId={deckId}&newOnly=true");
        Assert.Equal([newId], fresh!.Cards.Select(card => card.Id));

        var reviewRequest = new SubmitReviewRequest
        {
            ClientEventId = Guid.NewGuid(), CardId = dueId, Rating = SrsRating.Good, ReviewedAt = DateTime.UtcNow
        };
        var reviewed = await client.PostAsJsonAsync("/api/v1/reviews", reviewRequest);
        Assert.Equal(HttpStatusCode.OK, reviewed.StatusCode);
        var retried = await client.PostAsJsonAsync("/api/v1/reviews", reviewRequest);
        Assert.True((await retried.Content.ReadFromJsonAsync<ReviewResultResponse>())!.IsDuplicate);
        overview = await client.GetFromJsonAsync<ReviewOverviewResponse>($"/api/v1/reviews/overview?deckId={deckId}");
        Assert.Equal(0, overview!.DueCount);
        Assert.Equal(1, overview.UnseenCount);
        Assert.Equal(2, overview.LearningCount);
        Assert.Equal(1, overview.ObservedDueReviews);
        Assert.Equal(100, overview.ObservedRecallPercent);
    }

    [Fact]
    public async Task LessonFilter_IncludesDeduplicatedCardFromAnotherLesson()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        Guid firstLessonId, secondLessonId, cardId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(item => item.Email == "learner@languagelearning.app");
            var language = await db.Languages.FirstAsync(item => item.Code == "en");
            var unit = await db.Units.FirstAsync();
            user.StudyTargetJson = JsonSerializer.Serialize(new StudyTargetDto
            {
                LanguageId = language.Id, LanguageCode = "en", Level = "A1", Domain = "general", Domains = ["general"]
            });
            firstLessonId = Guid.NewGuid();
            secondLessonId = Guid.NewGuid();
            cardId = Guid.NewGuid();
            db.Lessons.AddRange(
                new Lesson { Id = firstLessonId, UnitId = unit.Id, Title = "First source lesson", SortOrder = 80 },
                new Lesson { Id = secondLessonId, UnitId = unit.Id, Title = "Second source lesson", SortOrder = 81 });
            db.Decks.Add(new Deck
            {
                Id = Guid.NewGuid(), OwnerId = user.Id, LanguageId = language.Id, Title = "First vocabulary deck",
                Tags = [new DeckTag { Id = Guid.NewGuid(), Tag = "course-level:a1" }],
                Cards = [new Card { Id = cardId, Front = "shared-source-test", Back = "shared meaning" }]
            });
            db.CardLessonSources.AddRange(
                new CardLessonSource { Id = Guid.NewGuid(), UserId = user.Id, LessonId = firstLessonId, CardId = cardId },
                new CardLessonSource { Id = Guid.NewGuid(), UserId = user.Id, LessonId = secondLessonId, CardId = cardId });
            await db.SaveChangesAsync();
        }

        var lessons = await client.GetFromJsonAsync<List<ReviewLessonSourceResponse>>("/api/v1/reviews/lessons");
        Assert.Contains(lessons!, item => item.LessonId == secondLessonId && item.LessonTitle == "Second source lesson" && item.CardCount == 1);
        var cards = await client.GetFromJsonAsync<DueCardsSummaryResponse>($"/api/v1/reviews/due?lessonId={secondLessonId}&newOnly=true");
        Assert.Contains(cards!.Cards, item => item.Id == cardId);
        var overview = await client.GetFromJsonAsync<ReviewOverviewResponse>($"/api/v1/reviews/overview?lessonId={secondLessonId}");
        Assert.Equal(1, overview!.TotalCount);
    }
}
