using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Tansekak.Application.Common;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Options;

namespace Tansekak.Infrastructure.Storage;

public class R2StorageService(IOptions<R2Options> options) : IR2Storage
{
    private readonly R2Options _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public Task<(string UploadUrl, string ObjectKey)> CreatePresignedUploadAsync(
        int yearId,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new ServiceUnavailableException(ApiErrorCodes.R2NotConfigured);

        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(ApiErrorCodes.OnlyXlsxFiles);

        var objectKey = $"imports/{yearId}/{Guid.NewGuid():N}{extension}";
        using var client = CreateClient();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var uploadUrl = client.GetPreSignedURL(request);
        return Task.FromResult((uploadUrl, objectKey));
    }

    public async Task UploadAsync(
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new ServiceUnavailableException(ApiErrorCodes.R2NotConfigured);

        ValidateObjectKey(objectKey);
        using var client = CreateClient();
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };

        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new ServiceUnavailableException(ApiErrorCodes.R2NotConfigured);

        ValidateObjectKey(objectKey);
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"tansekak-r2-{Guid.NewGuid():N}.xlsx");

        using var client = CreateClient();
        using var response = await client.GetObjectAsync(_options.BucketName, objectKey, cancellationToken);
        await using (var tempWrite = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         81920,
                         FileOptions.Asynchronous))
        {
            await response.ResponseStream.CopyToAsync(tempWrite, cancellationToken);
        }

        return OpenDeleteOnCloseReadStream(tempPath);
    }

    private static FileStream OpenDeleteOnCloseReadStream(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(objectKey))
            return;

        ValidateObjectKey(objectKey);
        using var client = CreateClient();
        await client.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);
    }

    private AmazonS3Client CreateClient()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        return new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
    }

    private static void ValidateObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)
            || objectKey.Contains("..", StringComparison.Ordinal)
            || objectKey.StartsWith('/'))
        {
            throw new ValidationException(ApiErrorCodes.InvalidObjectKey);
        }
    }
}
