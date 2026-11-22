using LanguageLearning.Application.DTOs.Media;

namespace LanguageLearning.Application.Interfaces;

public interface IMediaService
{
    Task<MediaUploadResponse> UploadAsync(Stream stream, string originalFileName, string contentType, long sizeBytes, CancellationToken cancellationToken = default);
    Task<(Stream stream, string contentType)?> GetFileAsync(string fileName, CancellationToken cancellationToken = default);
}
