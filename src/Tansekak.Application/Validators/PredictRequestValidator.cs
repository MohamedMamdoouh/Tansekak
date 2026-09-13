using FluentValidation;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class PredictRequestValidator : AbstractValidator<PredictRequestDto>
{
    public PredictRequestValidator()
    {
        RuleFor(x => x.Track).NotEmpty().Must(track => TrackHelper.TryParse(track, out _)).WithMessage("Invalid track.");
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationConstants.MaxPageSize);
    }
}
