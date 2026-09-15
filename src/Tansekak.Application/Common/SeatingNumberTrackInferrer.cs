using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class SeatingNumberTrackInferrer
{
    /// <summary>
    /// Infers academic track from the standard 2026 Thanaweya 7-digit seating number (2xxxxxx).
    /// The 2nd digit groups students by branch: 0-3 أدبي, 4-6 علمي رياضة, 7-9 علمي علوم.
    /// </summary>
    public static AcademicTrack? TryInferFromSeatingNo(string? seatingNo)
    {
        if (string.IsNullOrWhiteSpace(seatingNo))
            return null;

        var normalized = seatingNo.Trim();
        if (normalized.Length != 7 || normalized[0] != '2')
            return null;

        if (!char.IsDigit(normalized[1]))
            return null;

        return normalized[1] switch
        {
            '0' or '1' or '2' or '3' => AcademicTrack.Literature,
            '4' or '5' or '6' => AcademicTrack.Mathematics,
            '7' or '8' or '9' => AcademicTrack.Science,
            _ => null
        };
    }
}
