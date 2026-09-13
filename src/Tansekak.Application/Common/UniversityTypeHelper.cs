using Tansekak.Domain.Enums;

namespace Tansekak.Application.Common;

public static class UniversityTypeHelper
{
    public static bool TryParse(string? value, out UniversityType type)
    {
        type = UniversityType.Public;
        return Enum.TryParse(value, true, out type);
    }
}
