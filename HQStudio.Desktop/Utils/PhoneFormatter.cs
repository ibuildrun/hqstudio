using System.Linq;

namespace HQStudio.Desktop.Utils;

public static class PhoneFormatter
{
    /// <summary>
    /// Форматирует номер телефона в формат +7 (XXX) XXX-XX-XX
    /// </summary>
    public static string Format(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "";

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 0)
            return phone;

        if (digits.StartsWith("8") && digits.Length == 11)
            digits = "7" + digits[1..];

        if (digits.Length == 10)
            digits = "7" + digits;

        if (digits.Length == 11 && digits.StartsWith("7"))
        {
            return $"+7 ({digits[1..4]}) {digits[4..7]}-{digits[7..9]}-{digits[9..11]}";
        }

        if (digits.Length > 10)
        {
            return "+" + digits;
        }

        return phone;
    }

    /// <summary>
    /// Форматирует номер телефона в реальном времени при вводе
    /// </summary>
    public static string FormatAsYouType(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length == 0)
            return "";

        if (digits.StartsWith("8"))
            digits = "7" + digits[1..];

        return digits.Length switch
        {
            1 => $"+{digits}",
            >= 2 and <= 4 => $"+{digits[0]} ({digits[1..]}",
            >= 5 and <= 7 => $"+{digits[0]} ({digits[1..4]}) {digits[4..]}",
            >= 8 and <= 9 => $"+{digits[0]} ({digits[1..4]}) {digits[4..7]}-{digits[7..]}",
            >= 10 and <= 11 => $"+{digits[0]} ({digits[1..4]}) {digits[4..7]}-{digits[7..9]}-{digits[9..]}",
            _ => Format(input)
        };
    }
}
