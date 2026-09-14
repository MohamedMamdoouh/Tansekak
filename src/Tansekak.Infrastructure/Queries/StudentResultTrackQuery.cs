using Tansekak.Application.Common;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Queries;

internal static class StudentResultTrackQuery
{
    /// <summary>
    /// Filters peers for track ranking. All predicates are inlined so EF Core can translate
    /// them to SQL — custom C# helpers inside <c>Where</c> are not translatable.
    /// </summary>
    public static IQueryable<StudentResult> FilterByTrack(
        IQueryable<StudentResult> query,
        AcademicTrack track)
    {
        var canonical = TrackHelper.Canonical(track);
        return canonical switch
        {
            AcademicTrack.Science => query.Where(x =>
                x.Track == AcademicTrack.Science
                || x.Track == AcademicTrack.Mathematics
                || (x.Track == null && (
                    (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي علوم")
                    || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي رياضة")
                    || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي رياضه")
                    || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("الشعبة العلمية")
                    || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي")
                    || (
                        !(
                            (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي علوم")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي رياضة")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي رياضه")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("ادبي")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("الادبي")
                        )
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && (
                            x.SeatingNo.Trim().Substring(1, 1) == "4"
                            || x.SeatingNo.Trim().Substring(1, 1) == "5"
                            || x.SeatingNo.Trim().Substring(1, 1) == "6"
                            || x.SeatingNo.Trim().Substring(1, 1) == "7"
                            || x.SeatingNo.Trim().Substring(1, 1) == "8"
                            || x.SeatingNo.Trim().Substring(1, 1) == "9"
                        )
                    )
                ))),
            AcademicTrack.Literature => query.Where(x =>
                x.Track == AcademicTrack.Literature
                || (x.Track == null && (
                    (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("ادبي")
                    || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("الادبي")
                    || (
                        !(
                            (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي علوم")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي رياضة")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي رياضه")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("علمي")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("ادبي")
                            || (x.StudentCaseDesc ?? "").Trim().Replace("ى", "ي").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Contains("الادبي")
                        )
                        && x.SeatingNo.Trim().Length == 7
                        && x.SeatingNo.Trim().StartsWith("2")
                        && (
                            x.SeatingNo.Trim().Substring(1, 1) == "0"
                            || x.SeatingNo.Trim().Substring(1, 1) == "1"
                            || x.SeatingNo.Trim().Substring(1, 1) == "2"
                            || x.SeatingNo.Trim().Substring(1, 1) == "3"
                        )
                    )
                ))),
            _ => query.Where(_ => false)
        };
    }
}
