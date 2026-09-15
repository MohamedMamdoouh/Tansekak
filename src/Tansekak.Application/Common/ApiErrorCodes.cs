namespace Tansekak.Application.Common;

public static class ApiErrorCodes
{
    public const string InternalError = "INTERNAL_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string NotAuthenticated = "NOT_AUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string NoCurrentYear = "NO_CURRENT_YEAR";
    public const string InvalidTrack = "INVALID_TRACK";
    public const string InvalidUniversityType = "INVALID_UNIVERSITY_TYPE";
    public const string ScoreExceedsMax = "SCORE_EXCEEDS_MAX";
    public const string CutoffNegative = "CUTOFF_NEGATIVE";
    public const string CutoffDuplicate = "CUTOFF_DUPLICATE";
    public const string AdmissionYearNotFound = "ADMISSION_YEAR_NOT_FOUND";
    public const string AdmissionYearDuplicate = "ADMISSION_YEAR_DUPLICATE";
    public const string AdmissionYearLimitReached = "ADMISSION_YEAR_LIMIT_REACHED";
    public const string UniversityFacultyNotFound = "UNIVERSITY_FACULTY_NOT_FOUND";
    public const string StudentResultNotFound = "STUDENT_RESULT_NOT_FOUND";
    public const string FileRequired = "FILE_REQUIRED";
    public const string TrackRequired = "TRACK_REQUIRED";
    public const string OnlyMdFiles = "ONLY_MD_FILES";
    public const string AllowedTracksRequired = "ALLOWED_TRACKS_REQUIRED";
    public const string FacultyTrackNotAllowed = "FACULTY_TRACK_NOT_ALLOWED";

    public const string Required = "REQUIRED";
    public const string Invalid = "INVALID";
    public const string Empty = "EMPTY";
    public const string Duplicate = "DUPLICATE";
    public const string InvalidRow = "INVALID_ROW";
    public const string Malformed = "MALFORMED";
    public const string TrackNotAllowed = "TRACK_NOT_ALLOWED";
    public const string ScoreMustBePositive = "SCORE_MUST_BE_POSITIVE";
    public const string DuplicateRow = "DUPLICATE_ROW";
    public const string CollegeContainsScore = "COLLEGE_CONTAINS_SCORE";
    public const string CutoffMustBeNumber = "CUTOFF_MUST_BE_NUMBER";
    public const string FileNoDataRows = "FILE_NO_DATA_ROWS";
    public const string UnresolvedCollege = "UNRESOLVED_COLLEGE";

    public static readonly IReadOnlyCollection<string> All =
    [
        InternalError,
        NotFound,
        ValidationFailed,
        InvalidCredentials,
        NotAuthenticated,
        Forbidden,
        ServiceUnavailable,
        NoCurrentYear,
        InvalidTrack,
        InvalidUniversityType,
        ScoreExceedsMax,
        CutoffNegative,
        CutoffDuplicate,
        AdmissionYearNotFound,
        AdmissionYearDuplicate,
        AdmissionYearLimitReached,
        UniversityFacultyNotFound,
        StudentResultNotFound,
        FileRequired,
        TrackRequired,
        OnlyMdFiles,
        AllowedTracksRequired,
        FacultyTrackNotAllowed,
        Required,
        Invalid,
        Empty,
        Duplicate,
        InvalidRow,
        Malformed,
        TrackNotAllowed,
        ScoreMustBePositive,
        DuplicateRow,
        CollegeContainsScore,
        CutoffMustBeNumber,
        FileNoDataRows,
        UnresolvedCollege,
    ];
}
