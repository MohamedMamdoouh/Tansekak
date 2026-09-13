using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Persistence.Configurations;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        e.HasIndex(x => x.NameAr).IsUnique();
        e.Property(x => x.AllowedTracks)
            .HasConversion(
                v => FacultyAllowedTracksJson.Serialize(v),
                v => FacultyAllowedTracksJson.Deserialize(v),
                new ValueComparer<List<AcademicTrack>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (hash, track) => HashCode.Combine(hash, track)),
                    v => v.ToList()));
    }
}
