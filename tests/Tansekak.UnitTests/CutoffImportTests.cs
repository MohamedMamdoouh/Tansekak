using System.Text;
using Tansekak.Application.DTOs;
using Tansekak.Infrastructure.Import;

namespace Tansekak.UnitTests;

public class CutoffMarkdownParserTests
{
    [Fact]
    public void Parse_ValidTableRows_ReturnsRows()
    {
        const string md = """
            | الكلية | الحد الأدنى |
            | --- | --- |
            | طب القاهرة | 303.5 |
            | هندسة عين شمس | 295.0 |
            """;

        using var stream = ToStream(md);
        var (rows, errors) = CutoffMarkdownParser.Parse(stream);

        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
        Assert.Equal("طب القاهرة", rows[0].SourceLabel);
        Assert.Equal(303.5m, rows[0].CutoffScore);
    }

    [Fact]
    public void Parse_MalformedLabelWithLeadingScore_ReturnsError()
    {
        const string md = """
            | 295.5 صيدلة الزقازيق | 295.5 |
            """;

        using var stream = ToStream(md);
        var (_, errors) = CutoffMarkdownParser.Parse(stream);

        Assert.Single(errors);
        Assert.Equal("MALFORMED", errors[0].ErrorCode);
    }

    [Fact]
    public void Parse_InvalidScore_ReturnsError()
    {
        const string md = """
            | طب القاهرة | abc |
            """;

        using var stream = ToStream(md);
        var (rows, errors) = CutoffMarkdownParser.Parse(stream);

        Assert.Empty(rows);
        Assert.Single(errors);
        Assert.Equal("INVALID", errors[0].ErrorCode);
    }

    private static MemoryStream ToStream(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}

public class CutoffNameResolverTests
{
    private static readonly List<CutoffCatalogEntry> Catalog =
    [
        new(1, "جامعة القاهرة", "طب"),
        new(2, "جامعة عين شمس", "الإعلام"),
        new(3, "جامعة القاهرة", "الاقتصاد والعلوم السياسية"),
    ];

    [Fact]
    public void Resolve_ExactMedicineMatch_ReturnsEntry()
    {
        var resolver = new CutoffNameResolver(Catalog);
        var rows = new[] { new ParsedCutoffRow(4, "طب القاهرة", 303.5m) };

        var (resolved, unresolved) = resolver.Resolve(rows);

        Assert.Empty(unresolved);
        Assert.Single(resolved);
        Assert.Equal(1, resolved[0].UniversityFacultyId);
    }

    [Fact]
    public void Resolve_MediaMatch_ReturnsEntry()
    {
        var resolver = new CutoffNameResolver(Catalog);
        var rows = new[] { new ParsedCutoffRow(5, "إعلام عين شمس", 285m) };

        var (resolved, unresolved) = resolver.Resolve(rows);

        Assert.Empty(unresolved);
        Assert.Equal("جامعة عين شمس", resolved[0].UniversityNameAr);
    }

    [Fact]
    public void Resolve_OverrideMatch_ReturnsEntry()
    {
        var overrides = new[]
        {
            new CutoffNameOverride("اقتصاد و علوم سياسية القاهرة", "جامعة القاهرة", "الاقتصاد والعلوم السياسية")
        };
        var resolver = new CutoffNameResolver(Catalog, overrides);
        var rows = new[] { new ParsedCutoffRow(6, "اقتصاد و علوم سياسية القاهرة", 299.5m) };

        var (resolved, unresolved) = resolver.Resolve(rows);

        Assert.Empty(unresolved);
        Assert.Equal(3, resolved[0].UniversityFacultyId);
    }

    [Fact]
    public void Resolve_DentistryCompoundLabel_ReturnsEntry()
    {
        var catalog = new List<CutoffCatalogEntry>
        {
            new(10, "جامعة المنصورة", "أسنان"),
        };
        var resolver = new CutoffNameResolver(catalog);
        var rows = new[] { new ParsedCutoffRow(8, "طب أسنان المنصورة", 298m) };

        var (resolved, unresolved) = resolver.Resolve(rows);

        Assert.Empty(unresolved);
        Assert.Equal(10, resolved[0].UniversityFacultyId);
    }
    [Fact]
    public void Resolve_UnknownLabel_ReturnsUnresolved()
    {
        var resolver = new CutoffNameResolver(Catalog);
        var rows = new[] { new ParsedCutoffRow(7, "كلية غير موجودة", 200m) };

        var (_, unresolved) = resolver.Resolve(rows);

        Assert.Single(unresolved);
        Assert.Equal("UNRESOLVED", unresolved[0].ErrorCode);
    }
}
