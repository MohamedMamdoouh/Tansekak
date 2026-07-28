namespace Tansekak.Domain.Entities;

public class AdmissionYear
{
    public int Id { get; set; }
    public int Year { get; set; }
    public decimal MaximumScore { get; set; }
    public bool IsCurrent { get; set; }

    public ICollection<AdmissionCutoff> AdmissionCutoffs { get; set; } = [];
    public ICollection<StudentResult> StudentResults { get; set; } = [];
}
