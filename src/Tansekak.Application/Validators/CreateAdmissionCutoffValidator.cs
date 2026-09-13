using FluentValidation;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class CreateAdmissionCutoffValidator : AbstractValidator<CreateAdmissionCutoffDto>
{
    public CreateAdmissionCutoffValidator()
    {
        RuleFor(x => x.AdmissionYearId).GreaterThan(0);
        RuleFor(x => x.UniversityFacultyId).GreaterThan(0);
        RuleFor(x => x.Track).NotEmpty().Must(track => TrackHelper.TryParse(track, out _)).WithMessage("Invalid track.");
        RuleFor(x => x.CutoffScore).GreaterThanOrEqualTo(0);
    }
}

public class UpdateAdmissionCutoffValidator : AbstractValidator<UpdateAdmissionCutoffDto>
{
    public UpdateAdmissionCutoffValidator()
    {
        RuleFor(x => x.AdmissionYearId).GreaterThan(0);
        RuleFor(x => x.UniversityFacultyId).GreaterThan(0);
        RuleFor(x => x.Track).NotEmpty().Must(track => TrackHelper.TryParse(track, out _)).WithMessage("Invalid track.");
        RuleFor(x => x.CutoffScore).GreaterThanOrEqualTo(0);
    }
}
