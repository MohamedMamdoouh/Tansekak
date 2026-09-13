using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class UpdateAdmissionYearValidator : AbstractValidator<UpdateAdmissionYearDto>
{
    public UpdateAdmissionYearValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.MaximumScore).GreaterThan(0).LessThanOrEqualTo(1000);
    }
}
