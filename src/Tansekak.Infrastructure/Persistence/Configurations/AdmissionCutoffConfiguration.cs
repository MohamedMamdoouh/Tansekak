using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class AdmissionCutoffConfiguration : IEntityTypeConfiguration<AdmissionCutoff>
{
    public void Configure(EntityTypeBuilder<AdmissionCutoff> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.CutoffScore).HasPrecision(6, 2);
        e.HasIndex(x => x.Track);
        e.HasIndex(x => x.UniversityFacultyId);
        e.HasIndex(x => new { x.AdmissionYearId, x.UniversityFacultyId, x.Track }).IsUnique();
        e.HasOne(x => x.AdmissionYear).WithMany(x => x.AdmissionCutoffs)
            .HasForeignKey(x => x.AdmissionYearId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.UniversityFaculty).WithMany(x => x.AdmissionCutoffs)
            .HasForeignKey(x => x.UniversityFacultyId).OnDelete(DeleteBehavior.Restrict);
    }
}
