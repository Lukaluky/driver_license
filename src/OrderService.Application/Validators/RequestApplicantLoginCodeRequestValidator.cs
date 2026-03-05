using FluentValidation;
using OrderService.Application.DTOs.Auth;

namespace OrderService.Application.Validators;

public class RequestApplicantLoginCodeRequestValidator : AbstractValidator<RequestApplicantLoginCodeRequest>
{
    public RequestApplicantLoginCodeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email");
    }
}
