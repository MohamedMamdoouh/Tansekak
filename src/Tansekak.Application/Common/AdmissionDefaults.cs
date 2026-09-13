namespace Tansekak.Application.Common;

public static class AdmissionDefaults
{
    public const decimal MaximumScore = 320;

    public static int BootstrapYear(DateTime? utcNow = null) =>
        (utcNow ?? DateTime.UtcNow).Year;
}
