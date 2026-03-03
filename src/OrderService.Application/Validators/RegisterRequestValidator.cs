using FluentValidation;
using OrderService.Application.DTOs.Auth;
using OrderService.Application.Validation;

namespace OrderService.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов")
            .Matches("[A-Z]").WithMessage("Пароль должен содержать заглавную букву")
            .Matches("[a-z]").WithMessage("Пароль должен содержать строчную букву")
            .Matches("[0-9]").WithMessage("Пароль должен содержать цифру")
            .Matches("[^a-zA-Z0-9]").WithMessage("Пароль должен содержать спецсимвол");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Роль обязательна")
            .Must(r => r is "Applicant" or "Inspector")
            .WithMessage("Роль должна быть 'Applicant' или 'Inspector'");

        RuleFor(x => x.Iin)
            .NotEmpty().WithMessage("ИИН обязателен")
            .Length(12).WithMessage("ИИН должен содержать 12 цифр")
            .Matches(@"^\d{12}$").WithMessage("ИИН должен состоять только из цифр")
            .Must(IinUtils.IsValidFormat).WithMessage("Некорректный формат ИИН")
            .Must(iin => IinUtils.IsAtLeastAge(iin, 18)).WithMessage("Заявитель должен быть не младше 18 лет");
    }
}
