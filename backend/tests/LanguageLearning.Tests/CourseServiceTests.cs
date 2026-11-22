using LanguageLearning.Application.Common.Exceptions;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courseRepositoryMock;
    private readonly CourseService _courseService;

    public CourseServiceTests()
    {
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _courseService = new CourseService(_courseRepositoryMock.Object);
    }

    [Fact]
    public async Task GetCoursesAsync_ShouldReturnCoursesList()
    {
        var language = new Language { Id = Guid.NewGuid(), Code = "en", Name = "English" };
        var courses = new List<CourseResponse>
        {
            new CourseResponse
            {
                Id = Guid.NewGuid(),
                LanguageId = language.Id,
                LanguageName = language.Name,
                Title = "English A1",
                Level = "A1",
                Status = ContentStatus.Published
            }
        };

        _courseRepositoryMock.Setup(r => r.GetCourseSummariesAsync(null, null, null, null, default))
            .ReturnsAsync(courses);

        var result = await _courseService.GetCoursesAsync(null, null, null);

        Assert.Single(result);
        Assert.Equal("English A1", result[0].Title);
        Assert.Equal("English", result[0].LanguageName);
    }

    [Fact]
    public async Task GetCourseDetailAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        _courseRepositoryMock.Setup(r => r.GetCourseByIdAsync(It.IsAny<Guid>(), true, default))
            .ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _courseService.GetCourseDetailAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateCourseAsync_WhenLanguageNotFound_ShouldThrowNotFoundException()
    {
        _courseRepositoryMock.Setup(r => r.GetLanguageByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Language?)null);

        var request = new CreateCourseRequest
        {
            LanguageId = Guid.NewGuid(),
            Title = "Spanish Beginners",
            Level = "A1"
        };

        await Assert.ThrowsAsync<NotFoundException>(() => _courseService.CreateCourseAsync(request));
    }
}
