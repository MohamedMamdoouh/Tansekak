using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        e.HasOne(x => x.Governorate).WithMany(x => x.Universities)
            .HasForeignKey(x => x.GovernorateId).OnDelete(DeleteBehavior.Restrict);
    }
}
