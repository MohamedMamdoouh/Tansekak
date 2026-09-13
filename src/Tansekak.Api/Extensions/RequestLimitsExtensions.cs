using Microsoft.AspNetCore.Http.Features;

namespace Tansekak.Api.Extensions;

public static class RequestLimitsExtensions
{
    private const long MaxRequestBodySize = 104_857_600;

    public static WebApplicationBuilder ConfigureRequestLimits(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = MaxRequestBodySize;
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = MaxRequestBodySize;
        });

        return builder;
    }
}
