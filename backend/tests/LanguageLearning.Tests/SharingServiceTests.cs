using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class SharingServiceTests
{
    private readonly Mock<IDeckRepository> _deckRepositoryMock;
    private readonly Mock<ICardRepository> _cardRepositoryMock;
    private readonly SharingService _sharingService;

    public SharingServiceTests()
    {
        _deckRepositoryMock = new Mock<IDeckRepository>();
        _cardRepositoryMock = new Mock<ICardRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.IsAdmin()).Returns(true);
        _sharingService = new SharingService(_deckRepositoryMock.Object, _cardRepositoryMock.Object,
            new Mock<IStudyAccessService>().Object, currentUser.Object);
    }

    [Fact]
    public async Task CloneDeckAsync_WhenCloningOwnPublicDeck_ShouldCreatePrivateCopy()
    {
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var deck = new Deck { Id = deckId, OwnerId = ownerId, IsPublic = true, Title = "My deck" };

        _deckRepositoryMock.Setup(r => r.GetByIdAsync(deckId, default)).ReturnsAsync(deck);

        _cardRepositoryMock.Setup(r => r.GetByDeckIdAsync(deckId, default)).ReturnsAsync(new List<Card>());

        var result = await _sharingService.CloneDeckAsync(deckId, ownerId);

        Assert.Equal(deckId, result.SourceDeckId);
        _deckRepositoryMock.Verify(r => r.AddAsync(It.Is<Deck>(d => d.OwnerId == ownerId && !d.IsPublic && d.SourceDeckId == deckId), default), Times.Once);
    }

    [Fact]
    public async Task CloneDeckAsync_WithOtherPublicDeck_ShouldCreateIndependentCopy()
    {
        var ownerA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        var originalDeck = new Deck
        {
            Id = deckId,
            OwnerId = ownerA,
            Title = "Japanese N5",
            IsPublic = true,
            LanguageId = Guid.NewGuid(),
            Tags = new List<DeckTag> { new DeckTag { Tag = "Grammar" } }
        };

        var originalCards = new List<Card>
        {
            new Card { Id = Guid.NewGuid(), DeckId = deckId, Front = "Neko", Back = "Cat" },
            new Card { Id = Guid.NewGuid(), DeckId = deckId, Front = "Inu", Back = "Dog" }
        };

        _deckRepositoryMock.Setup(r => r.GetByIdAsync(deckId, default)).ReturnsAsync(originalDeck);
        _cardRepositoryMock.Setup(r => r.GetByDeckIdAsync(deckId, default)).ReturnsAsync(originalCards);

        var result = await _sharingService.CloneDeckAsync(deckId, userB);

        Assert.NotNull(result);
        Assert.NotEqual(deckId, result.ClonedDeckId);
        Assert.Equal(deckId, result.SourceDeckId);
        Assert.Equal(2, result.ClonedCardsCount);

        _deckRepositoryMock.Verify(r => r.AddAsync(It.Is<Deck>(d => d.OwnerId == userB && d.SourceDeckId == deckId), default), Times.Once);
        _cardRepositoryMock.Verify(r => r.AddBatchAsync(It.Is<IEnumerable<Card>>(c => c.Count() == 2), default), Times.Once);
    }
}
