using LanguageLearning.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace LanguageLearning.Tests.Services;

public sealed class MediaServiceTests
{
    [Fact]
    public async Task Upload_RejectsSpoofedMimeTypeWithoutSavingFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "language-learning-media-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = CreateService(folder);
            await using var stream = new MemoryStream("<script>alert(1)</script>"u8.ToArray());
            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(
                stream, "photo.png", "image/png", stream.Length));
            Assert.Empty(Directory.GetFiles(folder));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
                Directory.Delete(folder);
            }
        }
    }

    [Fact]
    public async Task Upload_SavesMatchingWebmAndRejectsIncorrectLength()
    {
        var folder = Path.Combine(Path.GetTempPath(), "language-learning-media-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = CreateService(folder);
            var bytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02 };
            await using var valid = new MemoryStream(bytes);
            var uploaded = await service.UploadAsync(valid, "recording.webm", "audio/webm", bytes.Length);
            Assert.EndsWith(".webm", uploaded.FileName);
            Assert.True(File.Exists(Path.Combine(folder, uploaded.FileName)));

            await using var mismatched = new MemoryStream(bytes);
            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(
                mismatched, "recording.webm", "audio/webm", bytes.Length + 1));
            Assert.Single(Directory.GetFiles(folder));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
                Directory.Delete(folder);
            }
        }
    }

    private static MediaService CreateService(string folder) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:StoragePath"] = folder })
        .Build());
}
