namespace LanguageLearning.Application.Interfaces;

public record StoredMedia(byte[] Content, string ContentType);

public interface IMediaStorage
{
    Task SaveAsync(Guid id, string extension, byte[] content, CancellationToken ct);
    Task<StoredMedia?> GetAsync(Guid id, CancellationToken ct);
}
