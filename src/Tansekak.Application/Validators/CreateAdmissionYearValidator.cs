using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateAdmissionYearValidator : AbstractValidator<CreateAdmissionYearDto>
{
    public CreateAdmissionYearValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("سنة القبول غير صحيحة.");
        RuleFor(x => x.MaximumScore)
            .GreaterThan(0).WithMessage("الحد الأقصى للمجموع يجب أن يكون أكبر من صفر.")
            .LessThanOrEqualTo(1000).WithMessage("الحد الأقصى للمجموع يتجاوز الحد المسموح.");
    }
}

public class UpdateAdmissionYearValidator : AbstractValidator<UpdateAdmissionYearDto>
{
    public UpdateAdmissionYearValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("سنة القبول غير صحيحة.");
        RuleFor(x => x.MaximumScore)
            .GreaterThan(0).WithMessage("الحد الأقصى للمجموع يجب أن يكون أكبر من صفر.")
            .LessThanOrEqualTo(1000).WithMessage("الحد الأقصى للمجموع يتجاوز الحد المسموح.");
    }
}
