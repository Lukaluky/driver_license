using FluentValidation;
using OrderService.Application.DTOs.Applications;

namespace OrderService.Application.Validators;

public class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID заявки обязателен");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Причина отклонения обязательна при отказе")
            .When(x => !x.Approved);
    }
}
