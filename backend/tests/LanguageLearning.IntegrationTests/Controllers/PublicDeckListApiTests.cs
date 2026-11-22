using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Cards;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Application.DTOs.Sharing;
using LanguageLearning.Application.Common;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class PublicDeckListApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicDeckListApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ListPublicDecks_FiltersInDatabaseAndReturnsCardCount()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await client.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var title = $"Public test {Guid.NewGuid():N}";
        var createdResponse = await client.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = title,
            Description = "Projection check",
            LanguageId = languages![0].Id,
            IsPublic = true,
            Tags = new List<string> { "integration" }
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var deck = await createdResponse.Content.ReadFromJsonAsync<DeckResponse>();
        Assert.NotNull(deck);

        var cardResponse = await client.PostAsJsonAsync($"/api/v1/decks/{deck.Id}/cards", new CreateCardRequest
        {
            Front = "Question",
            Back = "Answer"
        });
        Assert.Equal(HttpStatusCode.Created, cardResponse.StatusCode);

        var listed = await client.GetFromJsonAsync<List<PublicDeckResponse>>($"/api/v1/public/decks?languageId={deck.LanguageId}&search={Uri.EscapeDataString(title)}");
        var entry = Assert.Single(listed!);
        Assert.Equal(deck.Id, entry.Id);
        Assert.Equal(1, entry.CardsCount);
        Assert.Contains("integration", entry.Tags);

        var preview = await client.GetFromJsonAsync<PublicDeckPreviewResponse>($"/api/v1/public/decks/{deck.Id}/preview");
        Assert.NotNull(preview);
        Assert.Equal(1, preview.TotalCards);
        Assert.Single(preview.SampleCards);
        Assert.False(string.IsNullOrWhiteSpace(preview.LanguageName));
        Assert.False(string.IsNullOrWhiteSpace(preview.OwnerDisplayName));
    }

    [Fact]
    public async Task MyDecks_ExcludesOtherUsersPublicDecks()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var learner = await _factory.CreateAuthenticatedClientAsync();
        var languages = await owner.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var created = await owner.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = $"Other user public deck {Guid.NewGuid():N}",
            LanguageId = languages![0].Id,
            IsPublic = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var deck = await created.Content.ReadFromJsonAsync<DeckResponse>();
        Assert.NotNull(deck);

        var mine = await learner.GetFromJsonAsync<PagedResponse<DeckResponse>>("/api/v1/decks?page=1&pageSize=50");
        Assert.NotNull(mine);
        Assert.DoesNotContain(mine.Items, item => item.Id == deck.Id);

        var publicDecks = await learner.GetFromJsonAsync<List<PublicDeckResponse>>($"/api/v1/public/decks?search={Uri.EscapeDataString(deck.Title)}");
        Assert.DoesNotContain(publicDecks!, item => item.Id == deck.Id);
    }

    [Fact]
    public async Task ImportedAdminCourseDeck_IsNotListedAsCommunityDeck()
    {
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await admin.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var title = $"Legacy course deck {Guid.NewGuid():N}";
        var created = await admin.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = title, LanguageId = languages![0].Id, IsPublic = true,
            Tags = ["course-level:a1"]
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var deck = (await created.Content.ReadFromJsonAsync<DeckResponse>())!;
        var listed = await admin.GetFromJsonAsync<List<PublicDeckResponse>>($"/api/v1/public/decks?search={Uri.EscapeDataString(title)}");
        Assert.DoesNotContain(listed!, item => item.Id == deck.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/public/decks/{deck.Id}/preview")).StatusCode);
    }
}
