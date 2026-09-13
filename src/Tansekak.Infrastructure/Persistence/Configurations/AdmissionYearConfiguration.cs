using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class AdmissionYearConfiguration : IEntityTypeConfiguration<AdmissionYear>
{
    public void Configure(EntityTypeBuilder<AdmissionYear> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.MaximumScore).HasPrecision(6, 2);
        e.HasIndex(x => x.Year).IsUnique();
        e.HasIndex(x => x.IsCurrent)
            .IsUnique()
            .HasFilter("\"IsCurrent\" = true");
    }
}
