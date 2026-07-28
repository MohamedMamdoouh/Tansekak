namespace Tansekak.Domain.Entities;

public class UniversityFaculty
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public int FacultyId { get; set; }

    public University University { get; set; } = null!;
    public Faculty Faculty { get; set; } = null!;
    public ICollection<AdmissionCutoff> AdmissionCutoffs { get; set; } = [];
}
