using FluentValidation;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateAdmissionCutoffValidator : AbstractValidator<CreateAdmissionCutoffDto>
{
    public CreateAdmissionCutoffValidator()
    {
        RuleFor(x => x.AdmissionYearId).GreaterThan(0).WithMessage("سنة القبول غير صحيحة.");
        RuleFor(x => x.UniversityFacultyId).GreaterThan(0).WithMessage("كلية الجامعة غير صحيحة.");
        RuleFor(x => x.Track)
            .NotEmpty().WithMessage("الشعبة مطلوبة.")
            .Must(track => TrackHelper.TryParse(track, out _))
            .WithMessage("الشعبة غير صحيحة.");
        RuleFor(x => x.CutoffScore).GreaterThanOrEqualTo(0).WithMessage("يجب أن يكون الحد الأدنى صفراً أو أكثر.");
    }
}

public class UpdateAdmissionCutoffValidator : AbstractValidator<UpdateAdmissionCutoffDto>
{
    public UpdateAdmissionCutoffValidator()
    {
        RuleFor(x => x.AdmissionYearId).GreaterThan(0).WithMessage("سنة القبول غير صحيحة.");
        RuleFor(x => x.UniversityFacultyId).GreaterThan(0).WithMessage("كلية الجامعة غير صحيحة.");
        RuleFor(x => x.Track)
            .NotEmpty().WithMessage("الشعبة مطلوبة.")
            .Must(track => TrackHelper.TryParse(track, out _))
            .WithMessage("الشعبة غير صحيحة.");
        RuleFor(x => x.CutoffScore).GreaterThanOrEqualTo(0).WithMessage("يجب أن يكون الحد الأدنى صفراً أو أكثر.");
    }
}
