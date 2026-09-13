using FluentValidation;
using Tansekak.Application.DTOs;

namespace Tansekak.Application.Validators;

public class UpdateFacultyValidator : AbstractValidator<UpdateFacultyDto>
{
    public UpdateFacultyValidator() => RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
}
