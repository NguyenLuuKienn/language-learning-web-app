using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Progress;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class WritingSubmissionsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WritingSubmissionsApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SaveAndReadLatestDraft_UsesAccountDatabase()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var promptId = $"test-prompt-{Guid.NewGuid():N}";
        var text = "A short writing draft saved for this learner.";

        var request = new SaveWritingSubmissionRequest
        {
            PromptId = promptId,
            Text = text,
            ClientSubmissionId = Guid.NewGuid()
        };
        var save = await client.PostAsJsonAsync("/api/v1/me/writing-submissions", request);

        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var saved = await save.Content.ReadFromJsonAsync<WritingSubmissionDto>();
        Assert.NotNull(saved);
        Assert.Equal(text, saved.Text);
        var retried = await client.PostAsJsonAsync("/api/v1/me/writing-submissions", request);
        Assert.Equal(saved.Id, (await retried.Content.ReadFromJsonAsync<WritingSubmissionDto>())!.Id);

        var read = await client.GetAsync($"/api/v1/me/writing-submissions/latest?promptId={promptId}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var latest = await read.Content.ReadFromJsonAsync<WritingSubmissionDto>();
        Assert.NotNull(latest);
        Assert.Equal(saved.Id, latest.Id);
    }
}
