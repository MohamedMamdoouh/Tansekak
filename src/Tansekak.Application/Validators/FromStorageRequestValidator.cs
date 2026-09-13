using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class FromStorageRequestValidator : AbstractValidator<FromStorageRequestDto>
{
    public FromStorageRequestValidator() =>
        RuleFor(x => x.ObjectKey).NotEmpty().MaximumLength(500);
}
