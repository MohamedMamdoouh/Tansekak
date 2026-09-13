using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class UniversityFacultyConfiguration : IEntityTypeConfiguration<UniversityFaculty>
{
    public void Configure(EntityTypeBuilder<UniversityFaculty> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasIndex(x => x.UniversityId);
        e.HasIndex(x => x.FacultyId);
        e.HasIndex(x => new { x.UniversityId, x.FacultyId }).IsUnique();
        e.HasOne(x => x.University).WithMany(x => x.UniversityFaculties)
            .HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Faculty).WithMany(x => x.UniversityFaculties)
            .HasForeignKey(x => x.FacultyId).OnDelete(DeleteBehavior.Restrict);
    }
}
