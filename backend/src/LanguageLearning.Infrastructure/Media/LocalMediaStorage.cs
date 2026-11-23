using LanguageLearning.Application.Interfaces;

namespace LanguageLearning.Infrastructure.Media;

public class LocalMediaStorage : IMediaStorage
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes = new Dictionary<string, string>
    {
        ["png"] = "image/png", ["jpg"] = "image/jpeg", ["gif"] = "image/gif",
        ["webp"] = "image/webp", ["mp3"] = "audio/mpeg", ["wav"] = "audio/wav"
    };

    private readonly string _directory;

    public LocalMediaStorage(string directory) => _directory = directory;

    public async Task SaveAsync(Guid id, string extension, byte[] content, CancellationToken ct)
    {
        if (!ContentTypes.ContainsKey(extension)) throw new ArgumentException("Unsupported media type.");
        Directory.CreateDirectory(_directory);
        var temporaryPath = Path.Combine(_directory, $"{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                await output.WriteAsync(content, ct);
            File.Move(temporaryPath, Path.Combine(_directory, $"{id:N}.{extension}"));
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public async Task<StoredMedia?> GetAsync(Guid id, CancellationToken ct)
    {
        foreach (var (extension, contentType) in ContentTypes)
        {
            var path = Path.Combine(_directory, $"{id:N}.{extension}");
            if (File.Exists(path)) return new StoredMedia(await File.ReadAllBytesAsync(path, ct), contentType);
        }
        return null;
    }
}
