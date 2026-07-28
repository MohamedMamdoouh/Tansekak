namespace Tansekak.Domain.Entities;

public class Governorate
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;

    public ICollection<University> Universities { get; set; } = [];
}
