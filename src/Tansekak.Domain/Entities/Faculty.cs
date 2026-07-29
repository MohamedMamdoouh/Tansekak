using Tansekak.Domain.Enums;

namespace Tansekak.Domain.Entities;

public class Faculty
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public List<AcademicTrack> AllowedTracks { get; set; } = [];

    public ICollection<UniversityFaculty> UniversityFaculties { get; set; } = [];
}
