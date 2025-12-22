using System.Text.RegularExpressions;

namespace HQStudio.API.Services;

public static class PhoneFormatter
{
    /// <summary>
    /// Форматирует номер телефона в формат +7 (XXX) XXX-XX-XX
    /// </summary>
    public static string Format(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "";

        // Оставляем только цифры
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 0)
            return phone;

        // Если начинается с 8, заменяем на 7
        if (digits.StartsWith("8") && digits.Length == 11)
            digits = "7" + digits[1..];

        // Если 10 цифр, добавляем 7 в начало
        if (digits.Length == 10)
            digits = "7" + digits;

        // Форматируем если 11 цифр (российский номер)
        if (digits.Length == 11 && digits.StartsWith("7"))
        {
            return $"+7 ({digits[1..4]}) {digits[4..7]}-{digits[7..9]}-{digits[9..11]}";
        }

        // Для других форматов возвращаем с + если начинается с цифры
        if (digits.Length > 10)
        {
            return "+" + digits;
        }

        return phone;
    }

    /// <summary>
    /// Нормализует номер телефона (только цифры, начиная с 7)
    /// </summary>
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "";

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 0)
            return "";

        // Если начинается с 8, заменяем на 7
        if (digits.StartsWith("8") && digits.Length == 11)
            digits = "7" + digits[1..];

        // Если 10 цифр, добавляем 7 в начало
        if (digits.Length == 10)
            digits = "7" + digits;

        return digits;
    }
}
