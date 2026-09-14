using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateGovernorateValidator : AbstractValidator<CreateGovernorateDto>
{
    public CreateGovernorateValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم المحافظة مطلوب.")
            .MaximumLength(200).WithMessage("اسم المحافظة طويل جداً.");
    }
}
