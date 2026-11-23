using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Infrastructure.Media;
using Moq;

namespace LanguageLearning.Tests;

public class MediaServiceTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9FZMsAAAAASUVORK5CYII=");

    [Fact]
    public async Task ValidImageCanBeStoredAndRead()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"media-test-{Guid.NewGuid():N}");
        try
        {
            var service = new MediaService(new LocalMediaStorage(directory));
            var result = await service.UploadAsync(new MemoryStream(Png), "image/png", Png.Length, default);
            Assert.Equal($"/api/v1/media/{result.Id}", result.Url);
            var saved = await service.GetAsync(result.Id, default);
            Assert.NotNull(saved);
            Assert.Equal("image/png", saved.ContentType);
            Assert.Equal(Png, saved.Content);
            Assert.Null(await service.GetAsync(Guid.NewGuid(), default));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("application/x-msdownload")]
    [InlineData("text/html")]
    [InlineData("image/svg+xml")]
    public async Task DangerousMimeTypesAreRejected(string contentType)
    {
        var storage = new Mock<IMediaStorage>();
        var service = new MediaService(storage.Object);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(new MemoryStream(Png), contentType, Png.Length, default));
        storage.Verify(value => value.SaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecutableDisguisedAsPngIsRejected()
    {
        var storage = new Mock<IMediaStorage>();
        var service = new MediaService(storage.Object);
        byte[] executable = [77, 90, 0, 0, 0, 0, 0, 0];
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(new MemoryStream(executable), "image/png", executable.Length, default));
        storage.Verify(value => value.SaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HeaderOnlyPngIsRejected()
    {
        var service = new MediaService(new Mock<IMediaStorage>().Object);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(new MemoryStream(Png[..8]), "image/png", 8, default));
    }

    [Fact]
    public async Task OversizedAndMismatchedFilesAreRejected()
    {
        var service = new MediaService(new Mock<IMediaStorage>().Object);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(new MemoryStream(Png), "image/png", MediaService.MaxImageBytes + 1L, default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(new MemoryStream(Png), "image/png", Png.Length - 1, default));
    }
}
