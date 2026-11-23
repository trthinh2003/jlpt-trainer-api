using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JlptTrainer.Infrastructure.Media;

public sealed class S3MediaSettings
{
    public string Bucket { get; init; } = "";
    public string Region { get; init; } = "";
}

public sealed class S3MediaStorage(IOptions<S3MediaSettings> options, ILogger<S3MediaStorage> logger)
    : IMediaObjectStorage, IDisposable
{
    private readonly Lazy<AmazonS3Client> client = new(() => new AmazonS3Client(new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(options.Value.Region),
        RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
        MaxErrorRetry = 2,
        Timeout = TimeSpan.FromSeconds(20)
    }));

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.Bucket);

    public Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct) =>
        ExecuteAsync((s3, token) => s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.Bucket,
            Key = key,
            InputStream = stream,
            AutoCloseStream = false,
            ContentType = contentType,
            IfNoneMatch = "*"
        }, token), ct, TimeSpan.FromMinutes(2));

    public Task<string> SignReadAsync(string key, DateTimeOffset expiresAt, CancellationToken ct) =>
        ExecuteAsync((s3, token) => s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = options.Value.Bucket,
            Key = key,
            Expires = expiresAt.UtcDateTime,
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTPS,
            ResponseHeaderOverrides = new ResponseHeaderOverrides { CacheControl = "private, no-store" }
        }).WaitAsync(token), ct);

    public Task DeleteAsync(string key, CancellationToken ct) =>
        ExecuteAsync((s3, token) => s3.DeleteObjectAsync(options.Value.Bucket, key, token), ct);

    private async Task<T> ExecuteAsync<T>(Func<AmazonS3Client, CancellationToken, Task<T>> call, CancellationToken ct,
        TimeSpan? operationTimeout = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(operationTimeout ?? TimeSpan.FromSeconds(45));
        try
        {
            return await call(client.Value, timeout.Token);
        }
        catch (Exception ex) when (ex is Amazon.Runtime.AmazonClientException or Amazon.Runtime.AmazonServiceException
                                    or HttpRequestException or TimeoutException ||
                                    ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("S3 media operation failed ({ErrorType}).", ex.GetType().Name);
            throw new ExternalServiceException("S3", "Thao tác với kho tệp gặp lỗi. Vui lòng thử lại.");
        }
    }

    public void Dispose()
    {
        if (client.IsValueCreated) client.Value.Dispose();
    }
}
