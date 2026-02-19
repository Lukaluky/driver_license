using FluentValidation;
using OrderService.Application.DTOs.Applications;
using OrderService.Domain.Enums;

namespace OrderService.Application.Validators;

public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.Iin)
            .NotEmpty().WithMessage("ИИН обязателен")
            .Length(12).WithMessage("ИИН должен содержать 12 цифр")
            .Matches(@"^\d{12}$").WithMessage("ИИН должен состоять только из цифр")
            .Must(BeValidIinFormat).WithMessage("Некорректный формат ИИН")
            .Must(BeAtLeast18YearsOld).WithMessage("Заявитель должен быть не младше 18 лет");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО обязательно")
            .MinimumLength(3).WithMessage("ФИО должно содержать минимум 3 символа")
            .MaximumLength(200).WithMessage("ФИО не должно превышать 200 символов");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Категория обязательна")
            .Must(c => Enum.TryParse<LicenceCategory>(c, true, out _))
            .WithMessage("Допустимые категории: A, B, C, D, E");
    }

    /// <summary>
    /// IIN format: YYMMDD[G]XXXXX
    /// First 6 digits = birth date (YYMMDD)
    /// 7th digit (G) = century/gender indicator:
    ///   1,2 → 18xx | 3,4 → 19xx | 5,6 → 20xx
    /// </summary>
    private static bool BeValidIinFormat(string iin)
    {
        if (string.IsNullOrEmpty(iin) || iin.Length < 7) return false;

        int genderDigit = iin[6] - '0';
        if (genderDigit < 1 || genderDigit > 6) return false;

        if (!int.TryParse(iin[2..4], out int mm) || mm < 1 || mm > 12) return false;
        if (!int.TryParse(iin[4..6], out int dd) || dd < 1 || dd > 31) return false;

        return true;
    }

    private static bool BeAtLeast18YearsOld(string iin)
    {
        if (string.IsNullOrEmpty(iin) || iin.Length < 7) return false;

        if (!int.TryParse(iin[..2], out int yy)) return false;
        if (!int.TryParse(iin[2..4], out int mm)) return false;
        if (!int.TryParse(iin[4..6], out int dd)) return false;

        int genderDigit = iin[6] - '0';

        int year = genderDigit switch
        {
            1 or 2 => 1800 + yy,
            3 or 4 => 1900 + yy,
            5 or 6 => 2000 + yy,
            _ => 0
        };

        if (year == 0) return false;

        try
        {
            var birthDate = new DateOnly(year, mm, dd);
            var today = DateOnly.FromDateTime(DateTime.Today);
            int age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            return age >= 18;
        }
        catch
        {
            return false;
        }
    }
}
