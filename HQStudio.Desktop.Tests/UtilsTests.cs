using FluentAssertions;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для утилит Desktop приложения
/// </summary>
public class PhoneFormatterTests
{
    [Theory]
    [InlineData("79291234567", "+7 (929) 123-45-67")]
    [InlineData("89291234567", "+7 (929) 123-45-67")]
    [InlineData("9291234567", "+7 (929) 123-45-67")]
    [InlineData("+79291234567", "+7 (929) 123-45-67")]
    [InlineData("8 (929) 123-45-67", "+7 (929) 123-45-67")]
    public void Format_ValidPhone_ReturnsFormattedPhone(string input, string expected)
    {
        var result = TestPhoneFormatter.Format(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Format_EmptyInput_ReturnsEmpty(string? input, string expected)
    {
        var result = TestPhoneFormatter.Format(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("7", "+7")]
    [InlineData("79", "+7 (9")]
    [InlineData("792", "+7 (92")]
    [InlineData("7929", "+7 (929")]
    [InlineData("79291", "+7 (929) 1")]
    [InlineData("792912", "+7 (929) 12")]
    [InlineData("7929123", "+7 (929) 123")]
    [InlineData("79291234", "+7 (929) 123-4")]
    [InlineData("792912345", "+7 (929) 123-45")]
    [InlineData("7929123456", "+7 (929) 123-45-6")]
    [InlineData("79291234567", "+7 (929) 123-45-67")]
    public void FormatAsYouType_ReturnsProgressiveFormat(string input, string expected)
    {
        var result = TestPhoneFormatter.FormatAsYouType(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatAsYouType_EmptyInput_ReturnsEmpty()
    {
        TestPhoneFormatter.FormatAsYouType("").Should().Be("");
        TestPhoneFormatter.FormatAsYouType(null).Should().Be("");
    }
}

/// <summary>
/// Тесты для валидации ввода
/// </summary>
public class InputValidationTests
{
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.ru", true)]
    [InlineData("user+tag@example.org", true)]
    [InlineData("", true)] // Пустой email допустим
    [InlineData("   ", true)] // Пробелы = пустой
    [InlineData("invalid", false)]
    [InlineData("test@", false)]
    [InlineData("@domain.com", false)]
    public void IsValidEmail_ReturnsCorrectResult(string email, bool expected)
    {
        var result = TestInputValidation.IsValidEmail(email);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("+79291234567", true)]
    [InlineData("89291234567", true)]
    [InlineData("9291234567", true)]
    [InlineData("+7 (929) 123-45-67", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidPhone_ReturnsCorrectResult(string? phone, bool expected)
    {
        var result = TestInputValidation.IsValidPhone(phone);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("100", true)]
    [InlineData("100.50", true)]
    [InlineData("100,50", true)]
    [InlineData("1 000", true)]
    [InlineData("0", true)]
    [InlineData("", true)] // Пустая цена допустима
    [InlineData("-100", false)]
    [InlineData("abc", false)]
    public void IsValidPrice_ReturnsCorrectResult(string price, bool expected)
    {
        var result = TestInputValidation.IsValidPrice(price);
        result.Should().Be(expected);
    }
}

/// <summary>
/// Тесты для моделей
/// </summary>
public class ModelTests
{
    [Fact]
    public void Client_DefaultValues_AreCorrect()
    {
        var client = new TestClient();

        client.Id.Should().Be(0);
        client.Name.Should().BeEmpty();
        client.Phone.Should().BeEmpty();
        client.Car.Should().BeEmpty();
        client.CarNumber.Should().BeEmpty();
        client.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Order_StatusColor_ReturnsCorrectColor()
    {
        var order = new TestOrder();

        order.Status = "Новый";
        order.StatusColor.Should().Be("#2196F3");

        order.Status = "В работе";
        order.StatusColor.Should().Be("#FF9800");

        order.Status = "Завершен";
        order.StatusColor.Should().Be("#4CAF50");

        order.Status = "Отменен";
        order.StatusColor.Should().Be("#F44336");

        order.Status = "Unknown";
        order.StatusColor.Should().Be("#9E9E9E");
    }

    [Fact]
    public void Order_ServicesDisplay_ReturnsCorrectText()
    {
        var order = new TestOrder();

        // Без услуг
        order.ServicesDisplay.Should().Be("Не указаны");

        // С услугами
        order.Services.Add(new TestService { Name = "Шумоизоляция" });
        order.Services.Add(new TestService { Name = "Антихром" });
        order.ServicesDisplay.Should().Be("Шумоизоляция, Антихром");
    }

    [Fact]
    public void Order_ClientDisplay_ReturnsCorrectText()
    {
        var order = new TestOrder();

        // Без клиента
        order.ClientDisplay.Should().Be("Клиент не указан");

        // С именем клиента
        order.ClientName = "Иван Иванов";
        order.ClientDisplay.Should().Be("Иван Иванов");

        // С объектом клиента (но без имени)
        order.ClientName = "";
        order.Client = new TestClient { Name = "Пётр Петров" };
        order.ClientDisplay.Should().Be("Пётр Петров");
    }

    [Fact]
    public void Service_DefaultValues_AreCorrect()
    {
        var service = new TestService();

        service.Id.Should().Be(0);
        service.Name.Should().BeEmpty();
        service.Description.Should().BeEmpty();
        service.Category.Should().BeEmpty();
        service.PriceFrom.Should().Be(0);
        service.Icon.Should().Be("🔧");
        service.IsActive.Should().BeTrue();
    }
}

/// <summary>
/// Тесты для настроек приложения
/// </summary>
public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new TestAppSettings();

        settings.Theme.Should().Be("Dark");
        settings.ShowSplash.Should().BeTrue();
        settings.Language.Should().Be("ru");
        settings.ApiUrl.Should().Be("http://localhost:5000");
        settings.UseApi.Should().BeTrue();
    }

    [Theory]
    [InlineData("Dark", true)]
    [InlineData("Light", false)]
    [InlineData("dark", false)] // Case sensitive
    public void IsDarkTheme_ReturnsCorrectValue(string theme, bool expected)
    {
        var settings = new TestAppSettings { Theme = theme };
        settings.IsDarkTheme.Should().Be(expected);
    }
}

// Test implementations (копии из Desktop без WPF зависимостей)

public static class TestPhoneFormatter
{
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

    public static string FormatAsYouType(string? input)
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

public static class TestInputValidation
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        return digitsOnly.Length >= 10;
    }

    public static bool IsValidPrice(string? price)
    {
        if (string.IsNullOrWhiteSpace(price)) return true;
        var normalized = price.Replace(",", ".").Replace(" ", "");
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value) && value >= 0;
    }
}

public class TestClient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public string CarNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;
}

public class TestService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public string Icon { get; set; } = "🔧";
    public bool IsActive { get; set; } = true;
}

public class TestOrder
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public TestClient? Client { get; set; }
    public List<int> ServiceIds { get; set; } = new();
    public List<TestService> Services { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Новый";
    public string Notes { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;

    public string ServicesDisplay => Services.Any()
        ? string.Join(", ", Services.Select(s => s.Name))
        : "Не указаны";

    public string ClientDisplay => !string.IsNullOrEmpty(ClientName)
        ? ClientName
        : Client?.Name ?? "Клиент не указан";

    public string StatusColor => Status switch
    {
        "Новый" => "#2196F3",
        "В работе" => "#FF9800",
        "Завершен" => "#4CAF50",
        "Отменен" => "#F44336",
        _ => "#9E9E9E"
    };
}

public class TestAppSettings
{
    public string Theme { get; set; } = "Dark";
    public bool ShowSplash { get; set; } = true;
    public string Language { get; set; } = "ru";
    public string ApiUrl { get; set; } = "http://localhost:5000";
    public bool UseApi { get; set; } = true;

    public bool IsDarkTheme => Theme == "Dark";
}
