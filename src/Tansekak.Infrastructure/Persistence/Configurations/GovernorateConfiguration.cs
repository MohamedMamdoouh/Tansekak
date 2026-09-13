using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class GovernorateConfiguration : IEntityTypeConfiguration<Governorate>
{
    public void Configure(EntityTypeBuilder<Governorate> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        e.HasIndex(x => x.NameAr).IsUnique();
    }
}
