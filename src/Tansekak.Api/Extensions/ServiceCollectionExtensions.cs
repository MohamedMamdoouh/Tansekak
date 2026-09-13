using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;

namespace Tansekak.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        services.Configure<TansekakOptions>(configuration.GetSection(TansekakOptions.SectionName));
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError
                        {
                            Field = entry.Key,
                            Message = error.ErrorMessage
                        }))
                        .ToList();

                    var message = errors.FirstOrDefault()?.Message ?? "Validation failed.";
                    var response = ApiResponse<object>.Fail(message, errors);
                    return new BadRequestObjectResult(response);
                };
            });

        services.AddFluentValidationAutoValidation();
        services.AddOpenApi();

        var frontendOrigin = configuration["Frontend:Origin"];
        if (!string.IsNullOrWhiteSpace(frontendOrigin))
        {
            services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                    policy.WithOrigins(frontendOrigin)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });
        }

        return services;
    }
}
