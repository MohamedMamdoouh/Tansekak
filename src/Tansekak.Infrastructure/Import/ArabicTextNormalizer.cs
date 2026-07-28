using System.Text.RegularExpressions;

namespace Tansekak.Infrastructure.Import;

internal static partial class ArabicTextNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var s = value.Trim();
        s = s.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا');
        s = s.Replace('ى', 'ي').Replace('ة', 'ه').Replace('ؤ', 'و').Replace('ئ', 'ي');
        s = Whitespace().Replace(s, " ");
        return s.ToLowerInvariant();
    }

    public static string NormalizeKey(string? value)
    {
        var s = Normalize(value);
        s = s.Replace(" و ", " ");
        s = s.Replace("ال", "");
        s = s.Replace(" ", "");
        return s;
    }

    public static string UniversityShortName(string universityNameAr)
    {
        var name = universityNameAr.Trim();
        foreach (var prefix in new[] { "جامعة ", "جامعه ", "الجامعة ", "كلية ", "معهد ", "المعهد ", "المدرسة ", "مدرسة " })
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                name = name[prefix.Length..].Trim();
        }

        return name;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
