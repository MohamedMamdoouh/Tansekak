using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class UpdateGovernorateValidator : AbstractValidator<UpdateGovernorateDto>
{
    public UpdateGovernorateValidator() => RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
}
