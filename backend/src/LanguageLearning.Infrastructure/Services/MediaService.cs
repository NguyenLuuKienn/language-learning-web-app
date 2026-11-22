using LanguageLearning.Application.DTOs.Media;
using LanguageLearning.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LanguageLearning.Infrastructure.Services;

public class MediaService : IMediaService
{
    private readonly string _storageFolder;
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "audio/mpeg",
        "audio/mp3",
        "audio/ogg",
        "audio/wav",
        "audio/webm"
    };

    public MediaService(IConfiguration configuration)
    {
        var configuredPath = configuration["Media:StoragePath"];
        _storageFolder = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage", "uploads");

        if (!Directory.Exists(_storageFolder))
        {
            Directory.CreateDirectory(_storageFolder);
        }
    }

    public async Task<MediaUploadResponse> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        const int maxBytes = 10 * 1024 * 1024;
        if (sizeBytes <= 0 || sizeBytes > maxBytes)
        {
            throw new ArgumentException("File must be between 1 byte and 10 MB.");
        }

        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new ArgumentException("Unsupported media type. Allowed types: JPEG, PNG, WEBP, MP3, OGG, WAV, WEBM.");
        }

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/ogg" => ".ogg",
            "audio/wav" => ".wav",
            "audio/webm" => ".webm",
            _ => throw new ArgumentException("Unsupported media type.")
        };

        var header = new byte[12];
        var headerLength = 0;
        while (headerLength < header.Length)
        {
            var read = await stream.ReadAsync(header.AsMemory(headerLength), cancellationToken);
            if (read == 0) break;
            headerLength += read;
        }
        if (!MatchesSignature(contentType, header.AsSpan(0, headerLength)))
            throw new ArgumentException("File content does not match its media type.");

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_storageFolder, safeFileName);
        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            await fileStream.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);
            var total = headerLength;
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > maxBytes) throw new ArgumentException("File size exceeds 10 MB limit.");
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            if (total != sizeBytes) throw new ArgumentException("File size does not match the uploaded content.");
        }
        catch
        {
            File.Delete(filePath);
            throw;
        }

        return new MediaUploadResponse
        {
            Url = $"/api/v1/media/{safeFileName}",
            FileName = safeFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes
        };
    }

    private static bool MatchesSignature(string contentType, ReadOnlySpan<byte> data)
    {
        static bool StartsWith(ReadOnlySpan<byte> value, params byte[] prefix) => value.StartsWith(prefix);
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => StartsWith(data, 0xFF, 0xD8, 0xFF),
            "image/png" => StartsWith(data, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            "image/webp" => data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8),
            "audio/mpeg" or "audio/mp3" => data.StartsWith("ID3"u8) || data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xE0) == 0xE0,
            "audio/ogg" => data.StartsWith("OggS"u8),
            "audio/wav" => data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WAVE"u8),
            "audio/webm" => StartsWith(data, 0x1A, 0x45, 0xDF, 0xA3),
            _ => false
        };
    }

    public Task<(Stream stream, string contentType)?> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var cleanFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(_storageFolder, cleanFileName);

        if (!File.Exists(filePath))
        {
            return Task.FromResult<(Stream stream, string contentType)?>(null);
        }

        var ext = Path.GetExtension(cleanFileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream stream, string contentType)?>((stream, contentType));
    }
}
