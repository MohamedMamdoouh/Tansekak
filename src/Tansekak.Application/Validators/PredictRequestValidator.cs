using FluentValidation;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class PredictRequestValidator : AbstractValidator<PredictRequestDto>
{
    public PredictRequestValidator()
    {
        RuleFor(x => x.Track)
            .NotEmpty().WithMessage("الشعبة مطلوبة.")
            .Must(track => TrackHelper.TryParse(track, out _))
            .WithMessage("الشعبة غير صحيحة.");
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0).WithMessage("المجموع يجب أن يكون صفراً أو أكثر.");
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("رقم الصفحة غير صحيح.");
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("حجم الصفحة غير صحيح.")
            .LessThanOrEqualTo(PaginationConstants.MaxPageSize)
            .WithMessage("حجم الصفحة يتجاوز الحد المسموح.");
    }
}
