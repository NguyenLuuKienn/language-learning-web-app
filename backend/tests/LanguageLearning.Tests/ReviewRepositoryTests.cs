
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LanguageLearning.Tests;

public class ReviewRepositoryTests
{
    [Fact]
    public async Task GetDueSchedulesAsync_ReturnsOnlyDueCardsForUserAndDeck()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var otherDeckId = Guid.NewGuid();

        var nowUtc = new DateTime(
            2026, 10, 28, 12, 0, 0,
            DateTimeKind.Utc);

        var user = new User
        {
            Id = userId,
            Email = "user@test.local"
        };

        var otherUser = new User
        {
            Id = otherUserId,
            Email = "other@test.local"
        };

        var language = new Language
        {
            Code = "en",
            Name = "English"
        };

        var deck = new Deck
        {
            Id = deckId,
            OwnerId = userId,
            Language = language,
            Title = "English Deck"
        };

        var otherDeck = new Deck
        {
            Id = otherDeckId,
            OwnerId = userId,
            Language = language,
            Title = "Other Deck"
        };

        var pastCard = new Card
        {
            Deck = deck,
            Front = "Hello",
            Back = "Xin chào"
        };

        var nowCard = new Card
        {
            Deck = deck,
            Front = "Goodbye",
            Back = "Tạm biệt"
        };

        var futureCard = new Card
        {
            Deck = deck,
            Front = "Apple",
            Back = "Quả táo"
        };

        var otherDeckCard = new Card
        {
            Deck = otherDeck,
            Front = "Book",
            Back = "Quyển sách"
        };

        context.Users.AddRange(user, otherUser);

        context.CardSchedules.AddRange(
            new CardSchedule
            {
                User = user,
                Card = pastCard,
                DueAt = nowUtc.AddDays(-1)
            },
            new CardSchedule
            {
                User = user,
                Card = nowCard,
                DueAt = nowUtc
            },
            new CardSchedule
            {
                User = user,
                Card = futureCard,
                DueAt = nowUtc.AddDays(1)
            },
            new CardSchedule
            {
                User = user,
                Card = otherDeckCard,
                DueAt = nowUtc.AddDays(-2)
            },
            new CardSchedule
            {
                User = otherUser,
                Card = pastCard,
                DueAt = nowUtc.AddDays(-3)
            }
        );

        await context.SaveChangesAsync();

        var repository = new ReviewRepository(context);

        var result = await repository.GetDueSchedulesAsync(
            userId,
            deckId,
            nowUtc
        );

        Assert.Equal(2, result.Count);

        Assert.Equal(pastCard.Id, result[0].CardId);
        Assert.Equal(nowCard.Id, result[1].CardId);

        Assert.All(result, schedule =>
        {
            Assert.Equal(userId, schedule.UserId);
            Assert.Equal(deckId, schedule.Card.DeckId);
            Assert.True(schedule.DueAt <= nowUtc);
        });
    }

    [Fact]
    public async Task AddReviewEventAsync_ShouldPersistEvent()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = "review-event@test.local"
        };

        var language = new Language
        {
            Code = "en",
            Name = "English"
        };

        var deck = new Deck
        {
            Owner = user,
            Language = language,
            Title = "Review Test Deck"
        };

        var card = new Card
        {
            Deck = deck,
            Front = "Hello",
            Back = "Xin chao"
        };

        var reviewEvent = new ReviewEvent
        {
            User = user,
            Card = card,
            ClientEventId = Guid.NewGuid(),
            ReviewedAt = DateTime.UtcNow,
            AlgorithmVersion = "srs-v1"
        };

        var repository = new ReviewRepository(context);

        await repository.AddReviewEventAsync(reviewEvent);
        await repository.SaveChangesAsync();

        var savedEvent = await context.ReviewEvents
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(reviewEvent.Id, savedEvent.Id);
        Assert.Equal(user.Id, savedEvent.UserId);
        Assert.Equal(card.Id, savedEvent.CardId);
        Assert.Equal("srs-v1", savedEvent.AlgorithmVersion);
    }

}
