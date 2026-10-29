using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class ReviewServiceTests
{
    [Fact]
    public async Task GetDueCardsAsync_ValidDeck_ReturnsDueCards()
    {
        var userId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = deckId,
            OwnerId = userId
        };

        var card = new Card
        {
            Id = cardId,
            DeckId = deckId,
            Front = "Hello",
            Back = "Xin chào"
        };

        var schedule = new CardSchedule
        {
            UserId = userId,
            CardId = cardId,
            Card = card,
            DueAt = DateTime.UtcNow.AddDays(-1),
            AlgorithmVersion = "srs-v1"
        };

        var reviewRepository = new Mock<IReviewRepository>();
        var deckRepository = new Mock<IDeckRepository>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        deckRepository
            .Setup(x => x.GetByIdAsync(
                deckId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deck);

        reviewRepository
            .Setup(x => x.GetDueSchedulesAsync(
                userId,
                deckId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CardSchedule> { schedule });

        var service = new ReviewService(
            reviewRepository.Object,
            deckRepository.Object,
            currentUserService.Object
        );

        var result = await service.GetDueCardsAsync(deckId);

        var dueCard = Assert.Single(result);

        Assert.Equal(cardId, dueCard.CardId);
        Assert.Equal(deckId, dueCard.DeckId);
        Assert.Equal("Hello", dueCard.Front);
        Assert.Equal("Xin chào", dueCard.Back);
        Assert.Equal("srs-v1", dueCard.AlgorithmVersion);
        Assert.Equal(schedule.DueAt, dueCard.DueAt);

        reviewRepository.Verify(x => x.GetDueSchedulesAsync(
            userId,
            deckId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

[Fact]
public async Task GetDueCardsAsync_PrivateDeckOfAnotherUser_ShouldThrow()
{
    var currentUserId = Guid.NewGuid();
    var anotherUserId = Guid.NewGuid();
    var deckId = Guid.NewGuid();

    var deck = new Deck
    {
        Id = deckId,
        OwnerId = anotherUserId,
        IsPublic = false
    };

    var reviewRepository = new Mock<IReviewRepository>();
    var deckRepository = new Mock<IDeckRepository>();
    var currentUserService = new Mock<ICurrentUserService>();

    currentUserService
        .Setup(x => x.GetRequiredUserId())
        .Returns(currentUserId);

    deckRepository
        .Setup(x => x.GetByIdAsync(
            deckId,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(deck);

    var service = new ReviewService(
        reviewRepository.Object,
        deckRepository.Object,
        currentUserService.Object
    );

    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.GetDueCardsAsync(deckId)
    );

    reviewRepository.Verify(x => x.GetDueSchedulesAsync(
        It.IsAny<Guid>(),
        It.IsAny<Guid>(),
        It.IsAny<DateTime>(),
        It.IsAny<CancellationToken>()),
        Times.Never);
}

}
