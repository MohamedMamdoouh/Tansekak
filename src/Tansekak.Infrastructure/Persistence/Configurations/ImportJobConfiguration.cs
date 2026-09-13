using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Status).HasMaxLength(20).IsRequired();
        e.Property(x => x.Message).HasMaxLength(2000);
        e.Property(x => x.ObjectKey).HasMaxLength(500);
        e.HasIndex(x => x.CreatedAtUtc);
        e.HasIndex(x => x.Status);
        e.HasOne(x => x.AdmissionYear).WithMany()
            .HasForeignKey(x => x.AdmissionYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
