using Tansekak.Domain.Enums;

namespace Tansekak.Domain.Entities;

public class AdmissionCutoff
{
    public int Id { get; set; }
    public int AdmissionYearId { get; set; }
    public int UniversityFacultyId { get; set; }
    public AcademicTrack Track { get; set; }
    public decimal CutoffScore { get; set; }

    public AdmissionYear AdmissionYear { get; set; } = null!;
    public UniversityFaculty UniversityFaculty { get; set; } = null!;
}
