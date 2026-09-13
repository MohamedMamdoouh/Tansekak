using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Queries;

internal static class StudentResultTrackQuery
{
    public static IQueryable<StudentResult> FilterByTrack(
        IQueryable<StudentResult> query,
        AcademicTrack track) =>
        track switch
        {
            AcademicTrack.Science => query.Where(x =>
                x.Track == AcademicTrack.Science ||
                (x.Track == null && (
                    Normalize(x.StudentCaseDesc).Contains("علمي علوم")
                    || (
                        !ContainsAnyTrackKeyword(x.StudentCaseDesc)
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && (x.SeatingNo.Trim().Substring(1, 1) == "7"
                            || x.SeatingNo.Trim().Substring(1, 1) == "8"
                            || x.SeatingNo.Trim().Substring(1, 1) == "9"))))),
            AcademicTrack.Mathematics => query.Where(x =>
                x.Track == AcademicTrack.Mathematics ||
                (x.Track == null && (
                    Normalize(x.StudentCaseDesc).Contains("علمي رياضة")
                    || Normalize(x.StudentCaseDesc).Contains("علمي رياضه")
                    || (
                        !ContainsAnyTrackKeyword(x.StudentCaseDesc)
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && (x.SeatingNo.Trim().Substring(1, 1) == "4"
                            || x.SeatingNo.Trim().Substring(1, 1) == "5"
                            || x.SeatingNo.Trim().Substring(1, 1) == "6"))))),
            AcademicTrack.Literature => query.Where(x =>
                x.Track == AcademicTrack.Literature ||
                (x.Track == null && (
                    Normalize(x.StudentCaseDesc).Contains("ادبي")
                    || Normalize(x.StudentCaseDesc).Contains("الادبي")
                    || (
                        !ContainsAnyTrackKeyword(x.StudentCaseDesc)
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && (x.SeatingNo.Trim().Substring(1, 1) == "0"
                            || x.SeatingNo.Trim().Substring(1, 1) == "1"
                            || x.SeatingNo.Trim().Substring(1, 1) == "2"
                            || x.SeatingNo.Trim().Substring(1, 1) == "3"))))),
            _ => query.Where(_ => false)
        };

    private static string Normalize(string? text) =>
        (text ?? string.Empty).Trim()
            .Replace("ى", "ي")
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا");

    private static bool ContainsAnyTrackKeyword(string? caseDesc)
    {
        var text = Normalize(caseDesc);
        return text.Contains("علمي علوم")
            || text.Contains("علمي رياضة")
            || text.Contains("علمي رياضه")
            || text.Contains("ادبي")
            || text.Contains("الادبي");
    }
}
