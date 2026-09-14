using FluentValidation;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateUniversityValidator : AbstractValidator<CreateUniversityDto>
{
    public CreateUniversityValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الجامعة مطلوب.")
            .MaximumLength(200).WithMessage("اسم الجامعة طويل جداً.");
        RuleFor(x => x.GovernorateId).GreaterThan(0).WithMessage("المحافظة غير صحيحة.");
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("نوع الجامعة مطلوب.")
            .Must(type => UniversityTypeHelper.TryParse(type, out _))
            .WithMessage("نوع الجامعة غير صحيح.");
    }
}
