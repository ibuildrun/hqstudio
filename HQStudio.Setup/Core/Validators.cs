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

    public const int MaxDomainLength = 253;
    public const int MaxDomainLabelLength = 63;

    public const string PunycodeMessage =
        "Домен нужно ввести латиницей (в виде xn--...). Преобразовать русское имя можно на сайте reg.ru.";

    /// <summary>Lower-cased, trimmed host name as it is stored.</summary>
    public static string NormalizeDomain(string? value) => (value ?? "").Trim().ToLowerInvariant();

    /// <summary>True when the text has non-ASCII letters, i.e. it needs the punycode form.</summary>
    public static bool IsNonAsciiDomain(string? value) => (value ?? "").Any(c => c > 127);

    /// <summary>
    /// Empty is fine (optional). A host name with at least one dot: letters, digits and inner dashes, no scheme,
    /// path, port or spaces. Upper case is accepted here and lowered when saved.
    /// </summary>
    public static string? Domain(string? value)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0)
            return null;

        if (v.Any(char.IsWhiteSpace) || v.Contains("://") || v.IndexOfAny(new[] { '/', '\\', '?', '#', ':', '@' }) >= 0)
            return "Укажите только имя домена, например crm.example.ru: без http://, пути и пробелов";
        if (IsNonAsciiDomain(v))
            return PunycodeMessage;

        v = v.ToLowerInvariant();
        if (v.Length > MaxDomainLength)
            return "Слишком длинное имя домена";
        if (!v.Contains('.'))
            return "Нужно полное имя с точкой, например crm.example.ru";

        var labels = v.Split('.');
        foreach (var label in labels)
        {
            if (label.Length is 0 or > MaxDomainLabelLength || !DomainLabel.IsMatch(label))
                return "Только латинские буквы, цифры, дефис и точки, например crm.example.ru";
        }

        // 192.168.0.1 and similar are addresses, not domains
        if (labels[^1].All(char.IsDigit))
            return "Это не похоже на домен. Пример: crm.example.ru";
        return null;
    }

    private static readonly Regex DomainLabel = new(@"^[a-z0-9]+(-+[a-z0-9]+)*$", RegexOptions.Compiled);

    public static string? Key(string? value, string what)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0)
            return null;
        return v.Any(char.IsWhiteSpace) ? $"В {what} не должно быть пробелов" : null;
    }
}
