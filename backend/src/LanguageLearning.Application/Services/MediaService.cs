using LanguageLearning.Application.Interfaces;
using System.Buffers.Binary;

namespace LanguageLearning.Application.Services;

public record MediaUploadResult(Guid Id, string Url);

public class MediaService
{
    public const int MaxImageBytes = 5 * 1024 * 1024;
    public const int MaxAudioBytes = 10 * 1024 * 1024;

    private readonly IMediaStorage _storage;

    public MediaService(IMediaStorage storage) => _storage = storage;

    public async Task<MediaUploadResult> UploadAsync(Stream source, string contentType, long length, CancellationToken ct)
    {
        var (extension, limit) = contentType.ToLowerInvariant() switch
        {
            "image/png" => ("png", MaxImageBytes),
            "image/jpeg" => ("jpg", MaxImageBytes),
            "image/gif" => ("gif", MaxImageBytes),
            "image/webp" => ("webp", MaxImageBytes),
            "audio/mpeg" => ("mp3", MaxAudioBytes),
            "audio/wav" => ("wav", MaxAudioBytes),
            _ => throw new ArgumentException("Only PNG, JPEG, GIF, WebP, MP3 and WAV files are allowed.")
        };
        if (length < 1 || length > limit) throw new ArgumentException($"File must be between 1 and {limit} bytes.");

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > limit) throw new ArgumentException("File exceeds the size limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        var content = buffer.ToArray();
        if (content.LongLength != length || !HasSignature(content, extension))
            throw new ArgumentException("File content does not match its declared size or MIME type.");

        var id = Guid.NewGuid();
        await _storage.SaveAsync(id, extension, content, ct);
        return new MediaUploadResult(id, $"/api/v1/media/{id}");
    }

    public Task<StoredMedia?> GetAsync(Guid id, CancellationToken ct) => _storage.GetAsync(id, ct);

    private static bool HasSignature(ReadOnlySpan<byte> data, string extension) => extension switch
    {
        "png" => IsPng(data),
        "jpg" => data.Length >= 6 && data.StartsWith(new byte[] { 255, 216, 255 }) &&
            data.Slice(2, data.Length - 4).IndexOf(new byte[] { 255, 218 }) >= 0 &&
            data[^2] == 255 && data[^1] == 217,
        "gif" => data.Length >= 14 && (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8)) &&
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2)) > 0 &&
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2)) > 0 && data[^1] == 0x3B,
        "webp" => IsRiff(data, "WEBP"u8) && data.Length >= 20 &&
            (data.Slice(12, 4).SequenceEqual("VP8 "u8) || data.Slice(12, 4).SequenceEqual("VP8L"u8) ||
                data.Slice(12, 4).SequenceEqual("VP8X"u8)),
        "mp3" => IsMp3(data),
        "wav" => IsRiff(data, "WAVE"u8) && data.IndexOf("fmt "u8) >= 0 && data.IndexOf("data"u8) >= 0,
        _ => false
    };

    private static bool IsPng(ReadOnlySpan<byte> data)
    {
        if (data.Length < 57 || !data.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return false;
        if (BinaryPrimitives.ReadUInt32BigEndian(data.Slice(8, 4)) != 13 ||
            !data.Slice(12, 4).SequenceEqual("IHDR"u8) ||
            BinaryPrimitives.ReadUInt32BigEndian(data.Slice(16, 4)) == 0 ||
            BinaryPrimitives.ReadUInt32BigEndian(data.Slice(20, 4)) == 0) return false;
        var offset = 8;
        var hasImageData = false;
        while (offset + 12 <= data.Length)
        {
            var size = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
            if (size > data.Length - offset - 12) return false;
            var kind = data.Slice(offset + 4, 4);
            if (kind.SequenceEqual("IDAT"u8)) hasImageData = true;
            offset += checked((int)size + 12);
            if (kind.SequenceEqual("IEND"u8)) return hasImageData && size == 0 && offset == data.Length;
        }
        return false;
    }

    private static bool IsRiff(ReadOnlySpan<byte> data, ReadOnlySpan<byte> format) =>
        data.Length >= 12 && data.StartsWith("RIFF"u8) &&
        BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4, 4)) == data.Length - 8 &&
        data.Slice(8, 4).SequenceEqual(format);

    private static bool IsMp3(ReadOnlySpan<byte> data)
    {
        var offset = 0;
        if (data.StartsWith("ID3"u8))
        {
            if (data.Length < 10 || data.Slice(6, 4).ToArray().Any(value => value > 127)) return false;
            offset = 10 + (data[6] << 21) + (data[7] << 14) + (data[8] << 7) + data[9];
        }
        return data.Length >= offset + 4 && data[offset] == 255 && (data[offset + 1] & 0xE0) == 0xE0 &&
            (data[offset + 1] & 0x18) != 0x08 && (data[offset + 2] & 0xF0) != 0xF0;
    }
}
