using Tansekak.Domain.Enums;

namespace Tansekak.Domain.Entities;

public class University
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int GovernorateId { get; set; }
    public UniversityType Type { get; set; }

    public Governorate Governorate { get; set; } = null!;
    public ICollection<UniversityFaculty> UniversityFaculties { get; set; } = [];
}
