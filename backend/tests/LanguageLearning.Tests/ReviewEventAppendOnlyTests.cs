
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LanguageLearning.Tests;

public class ReviewEventAppendOnlyTests
{
    [Fact]
    public async Task ReviewEvent_CanBeAdded_ButCannotBeModifiedOrDeleted()
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
            Email = "review-test@example.com"
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
            Title = "English Flashcards"
        };

        var card = new Card
        {
            Deck = deck,
            Front = "Hello",
            Back = "Xin chào"
        };

        var reviewEvent = new ReviewEvent
        {
            User = user,
            Card = card,
            ClientEventId = Guid.NewGuid(),
            ReviewedAt = DateTime.UtcNow,
            AlgorithmVersion = "srs-v1"
        };

        context.ReviewEvents.Add(reviewEvent);
        await context.SaveChangesAsync();

        var eventId = reviewEvent.Id;

        Assert.Equal(1, await context.ReviewEvents.CountAsync());

        reviewEvent.AlgorithmVersion = "srs-v2";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        context.ChangeTracker.Clear();

        var savedEvent = await context.ReviewEvents
            .SingleAsync(e => e.Id == eventId);

        context.ReviewEvents.Remove(savedEvent);

        Assert.Throws<InvalidOperationException>(
            () => context.SaveChanges());

        context.ChangeTracker.Clear();

        var originalEvent = await context.ReviewEvents
            .AsNoTracking()
            .SingleAsync(e => e.Id == eventId);

        Assert.Equal("srs-v1", originalEvent.AlgorithmVersion);
        Assert.Equal(1, await context.ReviewEvents.CountAsync());

        context.ChangeTracker.Clear();

        var savedCard = await context.Cards
            .SingleAsync(c => c.Id == card.Id);

        context.Cards.Remove(savedCard);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        context.ChangeTracker.Clear();

        Assert.Equal(1, await context.ReviewEvents.CountAsync());
        Assert.True(await context.Cards.AnyAsync(c => c.Id == card.Id));
    }
}
