namespace Tansekak.Infrastructure.Seeding;

public record SeedGovernorate(int Id, string NameAr);
public record SeedFaculty(int Id, string NameAr, List<string>? AllowedTracks);
public record SeedUniversity(int Id, string NameAr, int GovernorateId, string Type);
public record SeedUniversityFaculty(int Id, int UniversityId, int FacultyId);
