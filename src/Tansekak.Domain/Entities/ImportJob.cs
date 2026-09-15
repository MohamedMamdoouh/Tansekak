namespace Tansekak.Domain.Entities;

public class ImportJob
{
    public Guid Id { get; set; }
    public int AdmissionYearId { get; set; }
    public string Status { get; set; } = ImportJobStatus.Queued;
    public int? ImportedCount { get; set; }
    public string? Message { get; set; }
    public string? ObjectKey { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public AdmissionYear AdmissionYear { get; set; } = null!;
}

public static class ImportJobStatus
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}
