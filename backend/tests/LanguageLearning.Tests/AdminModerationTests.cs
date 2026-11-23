using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LanguageLearning.Tests;

public class AdminModerationTests
{
    [Fact]
    public async Task LockAndHidePersistAuditAndRemovePublicDeck()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var admin = new User { Email = "admin@example.com", Role = UserRole.Admin };
        var learner = new User { Email = "learner@example.com" };
        var language = new Language { Code = "en", Name = "English" };
        var deck = new Deck { Owner = learner, Language = language, Title = "Public", IsPublic = true };
        db.Users.Add(admin);
        db.Decks.Add(deck);
        await db.SaveChangesAsync();

        var current = new Mock<ICurrentUserService>();
        current.Setup(value => value.GetRequiredUserId()).Returns(admin.Id);
        var service = new AdminModerationService(new AdminModerationRepository(db), current.Object);

        await service.SetUserActiveAsync(learner.Id, false, default);
        await service.SetDeckHiddenAsync(deck.Id, true, default);

        Assert.False((await db.Users.SingleAsync(value => value.Id == learner.Id)).IsActive);
        Assert.False(await new AccountStatus(db).IsActiveAsync(learner.Id, default));
        Assert.True((await db.Decks.SingleAsync(value => value.Id == deck.Id)).IsModerated);
        Assert.False(deck.IsPublic);
        Assert.Null(await new SharingReadRepository(db).PreviewAsync(deck.Id, default));
        Assert.Equal(new[] { "UserLocked", "DeckHidden" },
            (await db.AuditLogs.OrderBy(value => value.CreatedAt).ToListAsync()).Select(value => value.Action));
        Assert.All(await db.AuditLogs.ToListAsync(), value => Assert.Equal(admin.Id, value.ActorId));

        await service.SetUserActiveAsync(learner.Id, true, default);
        Assert.True(await new AccountStatus(db).IsActiveAsync(learner.Id, default));
        await service.SetDeckHiddenAsync(deck.Id, false, default);
        Assert.False(deck.IsModerated);
        Assert.True(deck.IsPublic);
        Assert.NotNull(await new SharingReadRepository(db).PreviewAsync(deck.Id, default));
        Assert.Equal(4, await db.AuditLogs.CountAsync());
        await service.SetDeckHiddenAsync(deck.Id, false, default);
        Assert.Equal(5, await db.AuditLogs.CountAsync());
        await Assert.ThrowsAsync<ConflictException>(() => service.SetUserActiveAsync(admin.Id, false, default));
        var privateDeck = new Deck { OwnerId = learner.Id, LanguageId = language.Id, Title = "Private", IsPublic = false };
        db.Decks.Add(privateDeck);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => service.SetDeckHiddenAsync(privateDeck.Id, true, default));
        await Assert.ThrowsAsync<ConflictException>(() => service.SetDeckHiddenAsync(privateDeck.Id, false, default));
    }
}
