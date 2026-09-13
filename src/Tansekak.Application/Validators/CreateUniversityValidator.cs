using FluentValidation;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateUniversityValidator : AbstractValidator<CreateUniversityDto>
{
    public CreateUniversityValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GovernorateId).GreaterThan(0);
        RuleFor(x => x.Type).NotEmpty().Must(type => UniversityTypeHelper.TryParse(type, out _)).WithMessage("Invalid university type.");
    }
}
