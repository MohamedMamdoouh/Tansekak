namespace Tansekak.Application.DTOs;

public record ConfigDto(
    string AppName,
    int CurrentYear,
    decimal MaximumScore,
    IReadOnlyList<string> Tracks);

public record PredictRequestDto(string Track, decimal Score, int Page = 1, int PageSize = 10);

public record NamedEntityDto(string NameAr);

public record AdmissionResultDto(
    NamedEntityDto University,
    NamedEntityDto Faculty);

public record PredictResponseDto(
    IReadOnlyList<AdmissionResultDto> Results,
    bool HasMore,
    int TotalCount);

public record StudentResultDto(
    string SeatingNo,
    string ArabicName,
    decimal TotalDegree,
    string StudentCaseDesc,
    int Year,
    string? Track = null,
    int? TrackRank = null,
    int? TrackTotalStudents = null);
