using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Tansekak.Infrastructure.Identity;

namespace Tansekak.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<UniversityFaculty> UniversityFaculties => Set<UniversityFaculty>();
    public DbSet<AdmissionYear> AdmissionYears => Set<AdmissionYear>();
    public DbSet<AdmissionCutoff> AdmissionCutoffs => Set<AdmissionCutoff>();
    public DbSet<StudentResult> StudentResults => Set<StudentResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Governorate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.NameAr).IsUnique();
        });

        builder.Entity<University>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Governorate).WithMany(x => x.Universities)
                .HasForeignKey(x => x.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Faculty>(e =>
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
                        v => v.ToList()))
                .HasColumnType("jsonb");
        });

        builder.Entity<UniversityFaculty>(e =>
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
        });

        builder.Entity<AdmissionYear>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.MaximumScore).HasPrecision(6, 2);
            e.HasIndex(x => x.Year).IsUnique();
            e.HasIndex(x => x.IsCurrent);
        });

        builder.Entity<AdmissionCutoff>(e =>
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
        });

        builder.Entity<StudentResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SeatingNo).HasMaxLength(20).IsRequired();
            e.Property(x => x.ArabicName).HasMaxLength(300).IsRequired();
            e.Property(x => x.TotalDegree).HasPrecision(6, 2);
            e.Property(x => x.StudentCaseDesc).HasMaxLength(100).IsRequired();
            e.Property(x => x.Track);
            e.HasIndex(x => x.SeatingNo);
            e.HasIndex(x => new { x.AdmissionYearId, x.SeatingNo }).IsUnique();
            e.HasOne(x => x.AdmissionYear).WithMany(x => x.StudentResults)
                .HasForeignKey(x => x.AdmissionYearId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
