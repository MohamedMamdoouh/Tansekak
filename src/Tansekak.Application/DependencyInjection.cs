using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tansekak.Application.Validators;

namespace Tansekak.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PredictRequestValidator>();
        return services;
    }
}
