using Tansekak.Api.Extensions;
using Tansekak.Application;
using Tansekak.Infrastructure;

namespace Tansekak.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.ConfigureRequestLimits();
        builder.Services.AddApiServices(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

        return builder;
    }

    public static async Task UseApiPipelineAsync(this WebApplication app)
    {
        app.ConfigureApiMiddleware();
        await app.Services.SeedDatabaseAsync();
    }
}
