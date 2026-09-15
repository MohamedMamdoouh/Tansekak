using Tansekak.Application.Common;
using Tansekak.Domain.Enums;

namespace Tansekak.Infrastructure.Seeding;

public static class CutoffSeedFileResolver
{
    public static string? Resolve(string cutoffsDirectory, AcademicTrack track, int year)
    {
        if (!Directory.Exists(cutoffsDirectory))
            return null;

        var trackKey = TrackHelper.ToDisplayName(track).ToLowerInvariant();
        var exactPath = Path.Combine(cutoffsDirectory, $"{trackKey}-{year}.md");
        if (File.Exists(exactPath))
            return exactPath;

        return Directory
            .GetFiles(cutoffsDirectory, $"{trackKey}-*.md")
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
