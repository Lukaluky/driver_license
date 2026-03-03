using FluentValidation;
using OrderService.Application.DTOs.Applications;
using OrderService.Application.Validation;
using OrderService.Domain.Enums;

namespace OrderService.Application.Validators;

public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.Iin)
            .Must(iin => string.IsNullOrWhiteSpace(iin) || iin.Length == 12)
            .WithMessage("ИИН должен содержать 12 цифр")
            .Must(iin => string.IsNullOrWhiteSpace(iin) || iin.All(char.IsDigit))
            .WithMessage("ИИН должен состоять только из цифр")
            .Must(iin => string.IsNullOrWhiteSpace(iin) || IinUtils.IsValidFormat(iin))
            .WithMessage("Некорректный формат ИИН");

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
