using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class FacultyTrackValidator
{
    public static bool IsTrackAllowed(Faculty faculty, AcademicTrack track) =>
        faculty.AllowedTracks.Count > 0 && faculty.AllowedTracks.Contains(track);

    public static string BuildRejectionMessage(Faculty faculty, AcademicTrack track) =>
        $"كلية \"{faculty.NameAr}\" غير متاحة لشعبة {TrackHelper.ToArabicName(track)}.";

    public static void EnsureTrackAllowed(Faculty faculty, AcademicTrack track)
    {
        if (!IsTrackAllowed(faculty, track))
            throw new ArgumentException(BuildRejectionMessage(faculty, track));
    }
}
