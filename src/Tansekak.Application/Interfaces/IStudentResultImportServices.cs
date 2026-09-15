using Tansekak.Application.DTOs;

namespace Tansekak.Application.Interfaces;

public interface IStudentResultImportService
{
    Task<ImportResultDto> ImportAsync(
        int yearId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public interface IR2Storage
{
    bool IsConfigured { get; }

    Task<(string UploadUrl, string ObjectKey)> CreatePresignedUploadAsync(
        int yearId,
        string fileName,
        CancellationToken cancellationToken = default);

    Task UploadAsync(
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}

public interface IImportJobService
{
    Task<ImportJobDto> CreateQueuedJobAsync(
        int yearId,
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<ImportJobDto?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task PrepareQueueAsync(CancellationToken cancellationToken = default);

    Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
