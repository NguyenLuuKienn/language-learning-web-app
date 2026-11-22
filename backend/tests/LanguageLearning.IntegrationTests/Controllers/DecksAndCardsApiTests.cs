using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class DecksAndCardsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DecksAndCardsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeckAndCard_RejectBlankFieldsAndEmptyIdentifiers()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        using var emptyLanguage = await client.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = "Valid deck", LanguageId = Guid.Empty
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyLanguage.StatusCode);

        var languages = await client.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var languageId = Assert.Single(languages!, item => item.Code == "en").Id;
        using var blankTitle = await client.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = "   ", LanguageId = languageId
        });
        Assert.Equal(HttpStatusCode.BadRequest, blankTitle.StatusCode);

        using var deck = await client.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = "Validation deck", LanguageId = languageId
        });
        Assert.Equal(HttpStatusCode.Created, deck.StatusCode);
        var created = await deck.Content.ReadFromJsonAsync<DeckResponse>();
        Assert.NotNull(created);

        using var blankCard = await client.PostAsJsonAsync($"/api/v1/decks/{created.Id}/cards", new CreateCardRequest
        {
            Front = " ", Back = "answer"
        });
        Assert.Equal(HttpStatusCode.BadRequest, blankCard.StatusCode);
        using var emptyBatch = await client.PostAsJsonAsync($"/api/v1/decks/{created.Id}/cards/batch", new BatchCreateCardsRequest());
        Assert.Equal(HttpStatusCode.BadRequest, emptyBatch.StatusCode);
    }

    [Fact]
    public async Task DeckAndCard_FullCrudLifecycle_ExecutesSuccessfully()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");

        var langResponse = await client.GetAsync("/api/v1/languages");
        var languages = await langResponse.Content.ReadFromJsonAsync<List<LanguageResponse>>();
        var languageId = languages![0].Id;

        var createDeckRequest = new CreateDeckRequest
        {
            Title = "Integration Test Deck",
            Description = "Automated test deck description",
            LanguageId = languageId,
            IsPublic = false,
            Tags = new List<string> { "test", "automated" }
        };

        var deckResponse = await client.PostAsJsonAsync("/api/v1/decks", createDeckRequest);
        Assert.Equal(HttpStatusCode.Created, deckResponse.StatusCode);
        var createdDeck = await deckResponse.Content.ReadFromJsonAsync<DeckResponse>();
        Assert.NotNull(createdDeck);
        Assert.Equal("Integration Test Deck", createdDeck.Title);

        var createCardRequest = new CreateCardRequest
        {
            Front = "Automation Question",
            Back = "Automation Answer"
        };

        var cardResponse = await client.PostAsJsonAsync($"/api/v1/decks/{createdDeck.Id}/cards", createCardRequest);
        Assert.Equal(HttpStatusCode.Created, cardResponse.StatusCode);
        var createdCard = await cardResponse.Content.ReadFromJsonAsync<CardResponse>();
        Assert.NotNull(createdCard);
        Assert.Equal("Automation Question", createdCard.Front);

        var updateCardRequest = new UpdateCardRequest
        {
            Front = "Updated Automation Question",
            Back = "Updated Automation Answer"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/cards/{createdCard.Id}", updateCardRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCard = await updateResponse.Content.ReadFromJsonAsync<CardResponse>();
        Assert.NotNull(updatedCard);
        Assert.Equal("Updated Automation Question", updatedCard.Front);

        var deleteCardResponse = await client.DeleteAsync($"/api/v1/cards/{createdCard.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCardResponse.StatusCode);

        var deleteDeckResponse = await client.DeleteAsync($"/api/v1/decks/{createdDeck.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteDeckResponse.StatusCode);
    }
}
