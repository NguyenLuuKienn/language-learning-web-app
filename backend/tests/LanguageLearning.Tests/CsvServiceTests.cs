using System.Text;
using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Csv;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class CsvServiceTests
{
    private readonly Mock<ICardRepository> _cardRepositoryMock;
    private readonly Mock<IDeckRepository> _deckRepositoryMock;
    private readonly CsvService _csvService;

    public CsvServiceTests()
    {
        _cardRepositoryMock = new Mock<ICardRepository>();
        _deckRepositoryMock = new Mock<IDeckRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.IsAdmin()).Returns(true);
        _csvService = new CsvService(_cardRepositoryMock.Object, _deckRepositoryMock.Object,
            new Mock<IStudyAccessService>().Object, currentUser.Object);
    }

    [Fact]
    public async Task PreviewAsync_WithValidCsv_ShouldDetectRowsCorrectly()
    {
        var csvContent = "Front,Back\nHello,Xin chao\nWater,Nuoc\n,Missing front";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        var result = await _csvService.PreviewAsync(stream);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.ValidRowsCount);
        Assert.Equal(1, result.InvalidRowsCount);
        Assert.False(result.Rows[2].IsValid);
    }

    [Fact]
    public async Task ExportDeckAsync_ShouldEscapeFormulaInjection()
    {
        var deckId = Guid.NewGuid();
        var deck = new Deck { Id = deckId, Title = "Test Deck" };
        var cards = new List<Card>
        {
            new Card { Id = Guid.NewGuid(), DeckId = deckId, Front = "=cmd|'/c calc'!A0", Back = "Dangerous formula" },
            new Card { Id = Guid.NewGuid(), DeckId = deckId, Front = "Normal Word", Back = "Normal Meaning" }
        };

        _deckRepositoryMock.Setup(r => r.GetByIdAsync(deckId, default)).ReturnsAsync(deck);
        _cardRepositoryMock.Setup(r => r.GetByDeckIdAsync(deckId, default)).ReturnsAsync(cards);

        var bytes = await _csvService.ExportDeckAsync(deckId, Guid.NewGuid());
        var csvText = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"'=cmd|'/c calc'!A0\"", csvText);
        Assert.Contains("\"Normal Word\"", csvText);
    }
}
