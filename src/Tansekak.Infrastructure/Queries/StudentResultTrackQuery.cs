using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Queries;

internal static class StudentResultTrackQuery
{
    public static IQueryable<StudentResult> FilterByTrack(
        IQueryable<StudentResult> query,
        AcademicTrack track)
    {
        var canonical = TrackHelper.Canonical(track);
        return canonical switch
        {
            AcademicTrack.Science => query.Where(x =>
                x.Track == AcademicTrack.Science ||
                x.Track == AcademicTrack.Mathematics ||
                (x.Track == null && (
                    IsScientificCaseDesc(x.StudentCaseDesc)
                    || (
                        !ContainsAnyTrackKeyword(x.StudentCaseDesc)
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && IsScientificSeatingDigit(x.SeatingNo.Trim().Substring(1, 1)))))),
            AcademicTrack.Literature => query.Where(x =>
                x.Track == AcademicTrack.Literature ||
                (x.Track == null && (
                    IsLiteraryCaseDesc(x.StudentCaseDesc)
                    || (
                        !ContainsAnyTrackKeyword(x.StudentCaseDesc)
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && IsLiterarySeatingDigit(x.SeatingNo.Trim().Substring(1, 1)))))),
            _ => query.Where(_ => false)
        };
    }

    private static bool IsScientificCaseDesc(string? caseDesc)
    {
        var text = Normalize(caseDesc);
        return text.Contains("علمي علوم")
            || text.Contains("علمي رياضة")
            || text.Contains("علمي رياضه")
            || text.Contains("الشعبة العلمية")
            || text.Contains("علمي");
    }

    private static bool IsLiteraryCaseDesc(string? caseDesc)
    {
        var text = Normalize(caseDesc);
        return text.Contains("ادبي") || text.Contains("الادبي");
    }

    private static bool IsScientificSeatingDigit(string digit) =>
        digit is "4" or "5" or "6" or "7" or "8" or "9";

    private static bool IsLiterarySeatingDigit(string digit) =>
        digit is "0" or "1" or "2" or "3";

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
            || text.Contains("علمي")
            || text.Contains("ادبي")
            || text.Contains("الادبي");
    }
}
