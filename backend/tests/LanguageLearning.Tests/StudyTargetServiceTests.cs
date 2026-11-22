using LanguageLearning.Application.DTOs.Auth;
using LanguageLearning.Application.DTOs.Courses;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using Moq;
using Xunit;

namespace LanguageLearning.Tests;

public class StudyTargetServiceTests
{
    [Fact]
    public async Task SaveAsync_AllowsAdminToSelectPublishedLevelWithoutPlacementProof()
    {
        var user = new User { Id = Guid.NewGuid(), Role = UserRole.Admin };
        var language = new Language { Id = Guid.NewGuid(), Code = "en", Name = "English" };
        var users = new Mock<IUserRepository>();
        var courses = new Mock<ICourseRepository>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        courses.Setup(repository => repository.GetLanguageByIdAsync(language.Id, default)).ReturnsAsync(language);
        courses.Setup(repository => repository.GetCourseSummariesAsync(language.Id, "A2", null, user.Id, default))
            .ReturnsAsync(new List<CourseResponse> { new() { Id = Guid.NewGuid(), LanguageId = language.Id, Level = "A2", Domain = "general", HasMaterial = true } });

        var result = await new StudyTargetService(users.Object, courses.Object, Mock.Of<IPlacementProofService>())
            .SaveAsync(user.Id, new StudyTargetDto { LanguageId = language.Id, Level = "A2", Domain = "general" });

        Assert.Equal("A2", result.Level);
        users.Verify(repository => repository.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_StoresValidatedCourseTargetForUser()
    {
        var user = new User { Id = Guid.NewGuid() };
        var language = new Language { Id = Guid.NewGuid(), Code = "en", Name = "English" };
        var users = new Mock<IUserRepository>();
        var courses = new Mock<ICourseRepository>();
        var proofs = new Mock<IPlacementProofService>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        courses.Setup(repository => repository.GetLanguageByIdAsync(language.Id, default)).ReturnsAsync(language);
        courses.Setup(repository => repository.GetCourseSummariesAsync(language.Id, "A1", null, user.Id, default))
            .ReturnsAsync(new List<CourseResponse> { new() { Id = Guid.NewGuid(), LanguageId = language.Id, Level = "A1", Domain = "general", HasMaterial = true } });
        proofs.Setup(service => service.IsValid("proof", user.Id, It.IsAny<Guid>())).Returns(true);

        var result = await new StudyTargetService(users.Object, courses.Object, proofs.Object).SaveAsync(user.Id,
            new StudyTargetDto { LanguageId = language.Id, Level = "A1", Domain = "general", PlacementProof = "proof" });

        Assert.Equal("A1", result.Level);
        Assert.Equal("en", result.LanguageCode);
        Assert.Contains("A1", user.StudyTargetJson);
        users.Verify(repository => repository.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_RejectsLevelWithoutPublishedMaterial()
    {
        var user = new User { Id = Guid.NewGuid() };
        var language = new Language { Id = Guid.NewGuid(), Code = "en" };
        var users = new Mock<IUserRepository>();
        var courses = new Mock<ICourseRepository>();
        var proofs = new Mock<IPlacementProofService>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        courses.Setup(repository => repository.GetLanguageByIdAsync(language.Id, default)).ReturnsAsync(language);
        courses.Setup(repository => repository.GetCourseSummariesAsync(language.Id, "A2", null, user.Id, default))
            .ReturnsAsync(new List<CourseResponse>());

        await Assert.ThrowsAsync<ArgumentException>(() => new StudyTargetService(users.Object, courses.Object, proofs.Object).SaveAsync(user.Id,
            new StudyTargetDto { LanguageId = language.Id, Level = "A2", Domain = "general" }));
        users.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_RejectsUnverifiedLevelEvenWhenCourseExists()
    {
        var user = new User { Id = Guid.NewGuid() };
        var language = new Language { Id = Guid.NewGuid(), Code = "en" };
        var users = new Mock<IUserRepository>();
        var courses = new Mock<ICourseRepository>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        courses.Setup(repository => repository.GetLanguageByIdAsync(language.Id, default)).ReturnsAsync(language);
        courses.Setup(repository => repository.GetCourseSummariesAsync(language.Id, "A2", null, user.Id, default))
            .ReturnsAsync(new List<CourseResponse> { new() { Id = Guid.NewGuid(), LanguageId = language.Id, Level = "A2", Domain = "general", HasMaterial = true } });

        await Assert.ThrowsAsync<ArgumentException>(() => new StudyTargetService(users.Object, courses.Object,
            Mock.Of<IPlacementProofService>()).SaveAsync(user.Id,
            new StudyTargetDto { LanguageId = language.Id, Level = "A2", Domain = "general" }));
        users.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }
}
