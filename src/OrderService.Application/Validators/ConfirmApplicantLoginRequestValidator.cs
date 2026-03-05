using FluentValidation;
using OrderService.Application.DTOs.Auth;

namespace OrderService.Application.Validators;

public class ConfirmApplicantLoginRequestValidator : AbstractValidator<ConfirmApplicantLoginRequest>
{
    public ConfirmApplicantLoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код обязателен")
            .Length(6).WithMessage("Код должен содержать 6 символов")
            .Matches(@"^\d{6}$").WithMessage("Код должен состоять только из цифр");
    }
}
