using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class FromStorageRequestValidator : AbstractValidator<FromStorageRequestDto>
{
    public FromStorageRequestValidator()
    {
        RuleFor(x => x.ObjectKey)
            .NotEmpty().WithMessage("مفتاح الملف مطلوب.")
            .MaximumLength(500).WithMessage("مفتاح الملف طويل جداً.");
    }
}
