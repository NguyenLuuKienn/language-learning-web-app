using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Notebook;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public sealed class NotebookApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public NotebookApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Notes_AreSavedOnlyAfterPost_AndOwnedByCreator()
    {
        using var guest = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/api/v1/notebook")).StatusCode);

        using var learner = await _factory.CreateAuthenticatedClientAsync();
        using var admin = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var empty = await learner.PostAsJsonAsync("/api/v1/notebook", new NotebookEntryRequest());
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var created = await learner.PostAsJsonAsync("/api/v1/notebook", new NotebookEntryRequest
        {
            Text = "community", Note = "Dùng trong bài về hàng xóm", LanguageCode = "en",
            SourcePath = "/courses", Context = "Our community meets every Friday."
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var entry = await created.Content.ReadFromJsonAsync<NotebookEntryResponse>();
        Assert.NotNull(entry);

        var learnerPage = await learner.GetFromJsonAsync<NotebookPage>("/api/v1/notebook?search=community");
        Assert.NotNull(learnerPage);
        Assert.Single(learnerPage.Items);
        Assert.Equal(entry.Id, learnerPage.Items[0].Id);

        var adminPage = await admin.GetFromJsonAsync<NotebookPage>("/api/v1/notebook");
        Assert.NotNull(adminPage);
        Assert.DoesNotContain(adminPage.Items, item => item.Id == entry.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/v1/notebook/" + entry.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await learner.DeleteAsync("/api/v1/notebook/" + entry.Id)).StatusCode);
    }

    private sealed class NotebookPage
    {
        public List<NotebookEntryResponse> Items { get; set; } = [];
    }
}
