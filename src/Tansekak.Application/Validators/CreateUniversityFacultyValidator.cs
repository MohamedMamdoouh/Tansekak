using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateUniversityFacultyValidator : AbstractValidator<CreateUniversityFacultyDto>
{
    public CreateUniversityFacultyValidator()
    {
        RuleFor(x => x.UniversityId).GreaterThan(0).WithMessage("الجامعة غير صحيحة.");
        RuleFor(x => x.FacultyId).GreaterThan(0).WithMessage("الكلية غير صحيحة.");
    }
}

public class UpdateUniversityFacultyValidator : AbstractValidator<UpdateUniversityFacultyDto>
{
    public UpdateUniversityFacultyValidator()
    {
        RuleFor(x => x.UniversityId).GreaterThan(0).WithMessage("الجامعة غير صحيحة.");
        RuleFor(x => x.FacultyId).GreaterThan(0).WithMessage("الكلية غير صحيحة.");
    }
}
