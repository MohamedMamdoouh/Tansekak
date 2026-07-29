using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Infrastructure.Services;

public class R2ImportStorageService
{
    public const long MaxFileSizeBytes = 104_857_600;
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly TimeSpan PresignedUrlLifetime = TimeSpan.FromHours(1);

    private readonly R2Options _options;
    private readonly IAmazonS3 _s3;
    private readonly ILogger<R2ImportStorageService> _logger;

    public R2ImportStorageService(
        IOptions<R2Options> options,
        IAmazonS3 s3,
        ILogger<R2ImportStorageService> logger)
    {
        _options = options.Value;
        _s3 = s3;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public ImportUploadUrlDto CreatePresignedUploadUrl(int yearId, string fileName, long totalSize)
    {
        EnsureConfigured();
        ValidateFile(fileName, totalSize);

        var objectKey = BuildObjectKey(yearId);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(PresignedUrlLifetime),
            ContentType = XlsxContentType,
        };

        var uploadUrl = _s3.GetPreSignedURL(request);
        _logger.LogInformation(
            "Created R2 presigned upload URL for year {YearId}, key {ObjectKey}, size {TotalSize}.",
            yearId,
            objectKey,
            totalSize);

        return new ImportUploadUrlDto(
            uploadUrl,
            objectKey,
            (int)PresignedUrlLifetime.TotalSeconds);
    }

    public async Task ValidateObjectAsync(string objectKey, int yearId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        EnsureObjectKeyForYear(objectKey, yearId);

        var response = await _s3.GetObjectMetadataAsync(_options.BucketName, objectKey, cancellationToken);
        if (response.ContentLength <= 0)
            throw new InvalidOperationException("Uploaded file is empty.");
        if (response.ContentLength > MaxFileSizeBytes)
            throw new InvalidOperationException($"Uploaded file exceeds {MaxFileSizeBytes} bytes.");
    }

    public async Task ExecuteWithObjectStreamAsync(
        string objectKey,
        Func<Stream, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var response = await _s3.GetObjectAsync(_options.BucketName, objectKey, cancellationToken);
        await action(response.ResponseStream, cancellationToken);
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            return;

        try
        {
            await _s3.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete R2 object {ObjectKey}.", objectKey);
        }
    }

    public static string BuildObjectKey(int yearId) =>
        $"imports/{yearId}/{Guid.NewGuid():N}.xlsx";

    public static void EnsureObjectKeyForYear(string objectKey, int yearId)
    {
        var expectedPrefix = $"imports/{yearId}/";
        if (!objectKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
            throw new ArgumentException("Invalid object key for this admission year.");
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException(
                "Direct file upload is not configured. Set R2 environment variables on the server.");
    }

    private static void ValidateFile(string fileName, long totalSize)
    {
        if (totalSize <= 0 || totalSize > MaxFileSizeBytes)
            throw new ArgumentException($"File size must be between 1 and {MaxFileSizeBytes} bytes.");

        var ext = Path.GetExtension(fileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .xlsx files are supported.");
    }
}
