namespace Tansekak.Domain.Entities;

public class Faculty
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;

    public ICollection<UniversityFaculty> UniversityFaculties { get; set; } = [];
}
