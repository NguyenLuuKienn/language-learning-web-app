
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class DeckServiceTests
{
    [Fact]
    public async Task UpdateDeck_OwnerCanUpdate_Success()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            LanguageId = Guid.NewGuid(),
            Title = "Old Title",
            Description = "Old Description",
            IsPublic = false,
            Version = 1
        };

        var repositoryMock = new Mock<IDeckRepository>();

        repositoryMock
            .Setup(repo => repo.GetByIdAsync(
                deckId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deck);

        repositoryMock
            .Setup(repo => repo.GetDeckStatisticsAsync(
                deckId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0, 0));

        var service = new DeckService(repositoryMock.Object);

        var request = new UpdateDeckRequest
        {
            Title = "New Title"
        };

        // Act
        var result = await service.UpdateDeckAsync(
            deckId, request, ownerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Title", deck.Title);

        repositoryMock.Verify(
            repo => repo.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDeck_NonOwnerCannotUpdate_TC03()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            LanguageId = Guid.NewGuid(),
            Title = "Owner Deck",
            Description = "Private Deck",
            IsPublic = false,
            Version = 1
        };

        var repositoryMock = new Mock<IDeckRepository>();

        repositoryMock
            .Setup(repo => repo.GetByIdAsync(
                deckId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deck);

        var service = new DeckService(repositoryMock.Object);

        var request = new UpdateDeckRequest
        {
            Title = "Unauthorized Change"
        };

        // Act
        var result = await service.UpdateDeckAsync(
            deckId, request, otherUserId);

        // Assert
        Assert.Null(result);
        Assert.Equal("Owner Deck", deck.Title);

        repositoryMock.Verify(
            repo => repo.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
[Fact]
public async Task DeleteDeck_OwnerCanDelete_Success()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = ownerId,
        LanguageId = Guid.NewGuid(),
        Title = "Owner Deck"
    };

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.GetByIdAsync(
            deckId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    var service = new DeckService(repositoryMock.Object);

    // Act
    var result = await service.DeleteDeckAsync(
        deckId, ownerId);

    // Assert
    Assert.True(result);

    repositoryMock.Verify(
        repo => repo.Delete(deck),
        Times.Once);

    repositoryMock.Verify(
        repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()),
        Times.Once);
}

[Fact]
public async Task DeleteDeck_NonOwnerCannotDelete_TC03()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = ownerId,
        LanguageId = Guid.NewGuid(),
        Title = "Owner Deck"
    };

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.GetByIdAsync(
            deckId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    var service = new DeckService(repositoryMock.Object);

    // Act
    var result = await service.DeleteDeckAsync(
        deckId, otherUserId);

    // Assert
    Assert.False(result);

    repositoryMock.Verify(
        repo => repo.Delete(It.IsAny<Deck>()),
        Times.Never);

    repositoryMock.Verify(
        repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()),
        Times.Never);
}

[Fact]
public async Task CreateDeck_NormalizesAndRemovesDuplicateTags()
{
    // Arrange
    var userId = Guid.NewGuid();
    var languageId = Guid.NewGuid();
    Deck? savedDeck = null;

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.LanguageExistsAsync(
            languageId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    repositoryMock
        .Setup(repo => repo.AddAsync(
            It.IsAny<Deck>(), It.IsAny<CancellationToken>()))
        .Callback<Deck, CancellationToken>(
            (deck, _) => savedDeck = deck)
        .Returns(Task.CompletedTask);

    var service = new DeckService(repositoryMock.Object);

    var request = new CreateDeckRequest
    {
        Title = "English Vocabulary",
        LanguageId = languageId,
        IsPublic = true,
        Tags = new List<string>
        {
            "English",
            " English ",
            "Grammar",
            "",
            "   "
        }
    };

    // Act
    var result = await service.CreateDeckAsync(request, userId);

    // Assert
    Assert.NotNull(savedDeck);
    Assert.Equal(userId, result.OwnerId);
    Assert.True(result.IsPublic);
    Assert.Equal(2, result.Tags.Count);
    Assert.Contains("English", result.Tags);
    Assert.Contains("Grammar", result.Tags);

    repositoryMock.Verify(
        repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()),
        Times.Once);
}

[Fact]
public async Task UpdateDeck_ReplacesTagsCorrectly()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = ownerId,
        LanguageId = Guid.NewGuid(),
        Title = "English Deck"
    };

    deck.Tags.Add(new DeckTag
    {
        Id = Guid.NewGuid(),
        Tag = "English"
    });

    deck.Tags.Add(new DeckTag
    {
        Id = Guid.NewGuid(),
        Tag = "Vocabulary"
    });

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.GetByIdAsync(
            deckId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    repositoryMock
        .Setup(repo => repo.GetDeckStatisticsAsync(
            deckId, ownerId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((0, 0, 0));

    var service = new DeckService(repositoryMock.Object);

    var request = new UpdateDeckRequest
    {
        Tags = new List<string>
        {
            "English",
            "Grammar",
            " Grammar "
        }
    };

    // Act
    var result = await service.UpdateDeckAsync(
        deckId, request, ownerId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Tags.Count);
    Assert.Contains("English", result.Tags);
    Assert.Contains("Grammar", result.Tags);
    Assert.DoesNotContain("Vocabulary", result.Tags);

    repositoryMock.Verify(
        repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()),
        Times.Once);
}

[Fact]
public async Task GetDeck_PublicDeckAccessibleToOtherUser()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = ownerId,
        LanguageId = Guid.NewGuid(),
        Title = "Public Deck",
        IsPublic = true
    };

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.GetByIdAsync(
            deckId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    repositoryMock
        .Setup(repo => repo.GetDeckStatisticsAsync(
            deckId, otherUserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((0, 0, 0));

    var service = new DeckService(repositoryMock.Object);

    // Act
    var result = await service.GetDeckByIdAsync(
        deckId, otherUserId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(deckId, result.Id);
    Assert.True(result.IsPublic);
}

[Fact]
public async Task GetDeck_PrivateDeckNotAccessibleToOtherUser()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = ownerId,
        LanguageId = Guid.NewGuid(),
        Title = "Private Deck",
        IsPublic = false
    };

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.GetByIdAsync(
            deckId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    var service = new DeckService(repositoryMock.Object);

    // Act
    var result = await service.GetDeckByIdAsync(
        deckId, otherUserId);

    // Assert
    Assert.Null(result);

    repositoryMock.Verify(
        repo => repo.GetDeckStatisticsAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}

[Fact]
public async Task GetDeckById_ReturnsCorrectCardStatistics()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = ownerId,
        LanguageId = Guid.NewGuid(),
        Title = "English Vocabulary",
        IsPublic = false
    };

    var repositoryMock = new Mock<IDeckRepository>();

    repositoryMock
        .Setup(repo => repo.GetByIdAsync(
            deckId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    // Giả lập Repository trả về:
    // 10 thẻ tổng cộng, 4 thẻ mới, 3 thẻ đến hạn
    repositoryMock
        .Setup(repo => repo.GetDeckStatisticsAsync(
            deckId,
            ownerId,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((10, 4, 3));

    var service = new DeckService(repositoryMock.Object);

    // Act
    var result = await service.GetDeckByIdAsync(
        deckId, ownerId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(10, result.TotalCards);
    Assert.Equal(4, result.NewCards);
    Assert.Equal(3, result.DueCards);

    repositoryMock.Verify(
        repo => repo.GetDeckStatisticsAsync(
            deckId,
            ownerId,
            It.IsAny<CancellationToken>()),
        Times.Once);
}

}
