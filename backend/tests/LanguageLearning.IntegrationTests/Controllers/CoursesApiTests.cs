using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests.Controllers;

public class CoursesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CoursesApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLanguages_ReturnsActiveLanguagesList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/languages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var languages = await response.Content.ReadFromJsonAsync<List<LanguageResponse>>();
        Assert.NotNull(languages);
        Assert.True(languages.Count >= 2);
        Assert.Contains(languages, l => l.Code == "en");
        Assert.Contains(languages, l => l.Code == "ja");
    }

    [Fact]
    public async Task GetCourses_WithoutFilter_ReturnsSeededCourses()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var courses = await response.Content.ReadFromJsonAsync<List<CourseResponse>>();
        Assert.NotNull(courses);
        Assert.NotEmpty(courses);
        Assert.Contains(courses, c => c.Title.Contains("English"));
    }

    [Fact]
    public async Task GetCourseDetail_WithValidCourseId_ReturnsUnitsAndLessons()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("admin@languagelearning.app", "Admin@123456");
        var coursesResponse = await client.GetAsync("/api/v1/courses");
        var courses = await coursesResponse.Content.ReadFromJsonAsync<List<CourseResponse>>();
        var firstCourse = Assert.Single(courses!);

        var response = await client.GetAsync($"/api/v1/courses/{firstCourse.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<CourseDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(firstCourse.Id, detail.Id);
        Assert.NotEmpty(detail.Units);
    }
}
