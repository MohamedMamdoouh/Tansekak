using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateFacultyValidator : AbstractValidator<CreateFacultyDto>
{
    public CreateFacultyValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الكلية مطلوب.")
            .MaximumLength(200).WithMessage("اسم الكلية طويل جداً.");
    }
}
