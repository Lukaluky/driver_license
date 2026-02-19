using FluentValidation;
using OrderService.Application.DTOs.Auth;

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
    }
}
