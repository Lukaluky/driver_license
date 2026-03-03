namespace OrderService.Application.Validation;

public static class IinUtils
{
    public static bool IsValidFormat(string iin)
    {
        if (string.IsNullOrWhiteSpace(iin) || iin.Length != 12) return false;
        if (!iin.All(char.IsDigit)) return false;

        var genderDigit = iin[6] - '0';
        if (genderDigit < 1 || genderDigit > 6) return false;
        if (!int.TryParse(iin[2..4], out var mm) || mm is < 1 or > 12) return false;
        if (!int.TryParse(iin[4..6], out var dd) || dd is < 1 or > 31) return false;

        return TryGetBirthDate(iin, out _);
    }

    public static bool TryGetBirthDate(string iin, out DateOnly birthDate)
    {
        birthDate = default;
        if (string.IsNullOrWhiteSpace(iin) || iin.Length < 7) return false;
        if (!int.TryParse(iin[..2], out var yy)) return false;
        if (!int.TryParse(iin[2..4], out var mm)) return false;
        if (!int.TryParse(iin[4..6], out var dd)) return false;

        var genderDigit = iin[6] - '0';
        var year = genderDigit switch
        {
            1 or 2 => 1800 + yy,
            3 or 4 => 1900 + yy,
            5 or 6 => 2000 + yy,
            _ => 0
        };
        if (year == 0) return false;

        try
        {
            birthDate = new DateOnly(year, mm, dd);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsAtLeastAge(string iin, int minAge)
    {
        if (!TryGetBirthDate(iin, out var birthDate)) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age >= minAge;
    }
}
