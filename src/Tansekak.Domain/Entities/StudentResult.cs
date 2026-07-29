using Tansekak.Domain.Enums;

namespace Tansekak.Domain.Entities;

public class StudentResult
{
    public int Id { get; set; }
    public int AdmissionYearId { get; set; }
    public string SeatingNo { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public decimal TotalDegree { get; set; }
    public string StudentCaseDesc { get; set; } = string.Empty;
    public AcademicTrack? Track { get; set; }

    public AdmissionYear AdmissionYear { get; set; } = null!;
}
