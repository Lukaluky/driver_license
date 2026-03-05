using FluentValidation;
using OrderService.Application.DTOs.Applications;
using OrderService.Domain.Enums;

namespace OrderService.Application.Validators;

public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО обязательно")
            .MinimumLength(3).WithMessage("ФИО должно содержать минимум 3 символа")
            .MaximumLength(200).WithMessage("ФИО не должно превышать 200 символов");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Категория обязательна")
            .Must(c => Enum.TryParse<LicenceCategory>(c, true, out _))
            .WithMessage("Допустимые категории: A, B, C, D, E");
    }

}
