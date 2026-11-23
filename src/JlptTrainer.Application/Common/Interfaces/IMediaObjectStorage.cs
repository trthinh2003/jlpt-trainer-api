namespace JlptTrainer.Application.Common.Interfaces;

public interface IMediaObjectStorage
{
    bool IsConfigured { get; }
    Task UploadAsync(string key, Stream stream, string contentType, CancellationToken cancellationToken);
    Task<string> SignReadAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
