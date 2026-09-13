using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class EntityIdSequenceConfiguration : IEntityTypeConfiguration<EntityIdSequence>
{
    public void Configure(EntityTypeBuilder<EntityIdSequence> e)
    {
        e.HasKey(x => x.Name);
        e.Property(x => x.Name).HasMaxLength(100);
    }
}
