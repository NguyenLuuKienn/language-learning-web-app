using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Sync;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.DTOs.Decks;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class SyncValidationApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SyncValidationApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SyncPush_RejectsUnknownOperationType()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        using var response = await client.PostAsJsonAsync("/api/v1/sync/push", new SyncPushRequest
        {
            Operations = [new SyncOperationItem
            {
                OperationId = Guid.NewGuid(),
                EntityId = Guid.NewGuid(),
                Type = "unsupported.operation",
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(new { })
            }]
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CardCreate_RetryAfterMissingLedger_DoesNotCreateAnotherCard()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var languages = await client.GetFromJsonAsync<List<LanguageResponse>>("/api/v1/languages");
        var languageId = Assert.Single(languages!, language => language.Code == "en").Id;
        using var deckResponse = await client.PostAsJsonAsync("/api/v1/decks", new CreateDeckRequest
        {
            Title = "Offline retry deck", LanguageId = languageId
        });
        deckResponse.EnsureSuccessStatusCode();
        var deck = await deckResponse.Content.ReadFromJsonAsync<DeckResponse>();
        Assert.NotNull(deck);
        var operationId = Guid.NewGuid();
        var request = new SyncPushRequest
        {
            Operations = [new SyncOperationItem
            {
                OperationId = operationId, EntityId = deck.Id, Type = "card.create",
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(new { deckId = deck.Id, front = "hello", back = "xin chào" })
            }]
        };
        using var first = await client.PostAsJsonAsync("/api/v1/sync/push", request);
        first.EnsureSuccessStatusCode();
        Assert.Equal("Accepted", Assert.Single((await first.Content.ReadFromJsonAsync<SyncPushResponse>())!.Results).Status);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ledger = await db.SyncOperations.SingleAsync(item => item.ClientOperationId == operationId);
            db.SyncOperations.Remove(ledger);
            await db.SaveChangesAsync();
        }

        using var retry = await client.PostAsJsonAsync("/api/v1/sync/push", request);
        retry.EnsureSuccessStatusCode();
        Assert.Equal("Accepted", Assert.Single((await retry.Content.ReadFromJsonAsync<SyncPushResponse>())!.Results).Status);
        using var cardsResponse = await client.GetAsync($"/api/v1/decks/{deck.Id}/cards");
        cardsResponse.EnsureSuccessStatusCode();
        var cards = await cardsResponse.Content.ReadFromJsonAsync<List<LanguageLearning.Application.DTOs.Cards.CardResponse>>();
        Assert.Equal(operationId, Assert.Single(cards!).Id);
    }
}
