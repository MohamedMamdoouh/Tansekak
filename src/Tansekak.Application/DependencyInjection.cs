using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tansekak.Application.DTOs;

namespace Tansekak.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PredictRequestValidator>();
        return services;
    }
}

public class PredictRequestValidator : AbstractValidator<PredictRequestDto>
{
    public PredictRequestValidator()
    {
        RuleFor(x => x.Track).NotEmpty();
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class CreateGovernorateValidator : AbstractValidator<CreateGovernorateDto>
{
    public CreateGovernorateValidator() => RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
}

public class CreateUniversityValidator : AbstractValidator<CreateUniversityDto>
{
    public CreateUniversityValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GovernorateId).GreaterThan(0);
        RuleFor(x => x.Type).NotEmpty();
    }
}

public class CreateFacultyValidator : AbstractValidator<CreateFacultyDto>
{
    public CreateFacultyValidator() => RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
