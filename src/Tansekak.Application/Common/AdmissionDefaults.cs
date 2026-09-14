namespace Tansekak.Application.Common;

public static class AdmissionDefaults
{
    public const decimal MaximumScore = 320;
    public const int DefaultYear = 2027;

    public static int BootstrapYear(DateTime? utcNow = null) => DefaultYear;
}
