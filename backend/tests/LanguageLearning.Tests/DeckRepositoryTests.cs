
using LanguageLearning.Domain.Entities;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LanguageLearning.Tests;

public class DeckRepositoryTests
{
    [Fact]
    public void DeleteDeck_ShouldMarkAsDeletedInsteadOfRemoving()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Database=unused;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        using var context = new AppDbContext(options);

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            LanguageId = Guid.NewGuid(),
            Title = "English Vocabulary",
            IsDeleted = false,
            DeletedAt = null,
            Version = 1
        };

        context.Decks.Attach(deck);

        var repository = new DeckRepository(context);

        var beforeDelete = DateTime.UtcNow;

        repository.Delete(deck);

        var afterDelete = DateTime.UtcNow;

        Assert.True(deck.IsDeleted);
        Assert.NotNull(deck.DeletedAt);
        Assert.InRange(
            deck.DeletedAt.Value,
            beforeDelete,
            afterDelete);

        Assert.Equal(2, deck.Version);

        Assert.Equal(
            EntityState.Modified,
            context.Entry(deck).State);
    }
}
