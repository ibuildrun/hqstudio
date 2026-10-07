using System.Text.RegularExpressions;

namespace HQStudio.Setup.Core;

public enum PasswordStrength
{
    None,
    Weak,
    Medium,
    Strong
}

/// <summary>Wizard input rules. Each method returns a ready-to-show Russian message or null when the value is fine.</summary>
public static class Validators
{
    public const int MinPasswordLength = 8;
    public const int MaxNameLength = 60;
    public const int MaxSubdomainLength = 40;

    private static readonly Regex SubdomainPattern = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678", "123456789", "1234567890", "qwertyui", "qwerty123", "password", "password1", "11111111", "admin123"
    };

    public static string? Name(string? value, string what)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0)
            return $"Введите {what}";
        if (v.Length > MaxNameLength)
            return "Слишком длинное значение";
        return null;
    }

    public static string? FirstName(string? value) => Name(value, "имя");

    public static string? LastName(string? value) => Name(value, "фамилию");

    public static string? Password(string? password, string? firstName, string? lastName)
    {
        var pw = password ?? "";
        if (pw.Length < MinPasswordLength)
            return $"Пароль должен быть не короче {MinPasswordLength} символов";
        if (string.IsNullOrWhiteSpace(pw))
            return "Пароль не может состоять из одних пробелов";

        var first = firstName?.Trim() ?? "";
        var last = lastName?.Trim() ?? "";
        var forbidden = new[] { first, last, $"{first} {last}".Trim(), $"{last} {first}".Trim(), $"{first}{last}", $"{last}{first}" };
        foreach (var name in forbidden)
        {
            if (name.Length > 0 && string.Equals(pw.Trim(), name, StringComparison.OrdinalIgnoreCase))
                return "Пароль не должен совпадать с именем или фамилией";
        }
        return null;
    }

    public static string? PasswordRepeat(string? password, string? repeat) =>
        string.Equals(password ?? "", repeat ?? "", StringComparison.Ordinal) ? null : "Пароли не совпадают";

    public static PasswordStrength Strength(string? password)
    {
        var pw = password ?? "";
        if (pw.Length == 0)
            return PasswordStrength.None;
        if (pw.Length < MinPasswordLength || CommonPasswords.Contains(pw))
            return PasswordStrength.Weak;

        var classes = 0;
        if (pw.Any(char.IsLower)) classes++;
        if (pw.Any(char.IsUpper)) classes++;
        if (pw.Any(char.IsDigit)) classes++;
        if (pw.Any(c => !char.IsLetterOrDigit(c))) classes++;

        if (pw.Length >= 12 && classes >= 3)
            return PasswordStrength.Strong;
        if (classes >= 2)
            return PasswordStrength.Medium;
        return PasswordStrength.Weak;
    }

    public static string StrengthText(PasswordStrength strength) => strength switch
    {
        PasswordStrength.Weak => "Слабый пароль: добавьте цифры и заглавные буквы",
        PasswordStrength.Medium => "Средний пароль: можно сделать длиннее",
        PasswordStrength.Strong => "Надёжный пароль",
        _ => "Не короче 8 символов, лучше с цифрами и заглавными буквами"
    };

    /// <summary>Empty is fine (the field is optional). Upper case is accepted here and lowered when saved.</summary>
    public static string? Subdomain(string? value)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0)
            return null;
        if (v.Length > MaxSubdomainLength)
            return $"Не длиннее {MaxSubdomainLength} символов";
        if (!SubdomainPattern.IsMatch(v.ToLowerInvariant()))
            return "Только латинские буквы, цифры и дефис (дефис не в начале и не в конце)";
        return null;
    }

    public static string? Key(string? value, string what)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0)
            return null;
        return v.Any(char.IsWhiteSpace) ? $"В {what} не должно быть пробелов" : null;
    }
}
