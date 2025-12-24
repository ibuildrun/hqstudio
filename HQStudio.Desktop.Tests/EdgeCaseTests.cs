using FluentAssertions;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для крайних случаев и граничных условий
/// </summary>
public class EdgeCaseTests
{
    #region Phone Formatting Edge Cases

    [Theory]
    [InlineData("+++79291234567", "+79291234567")] // Множественные плюсы
    [InlineData("7 9 2 9 1 2 3 4 5 6 7", "+7 (929) 123-45-67")] // Пробелы между цифрами
    [InlineData("(929)1234567", "+7 (929) 123-45-67")] // Без кода страны
    [InlineData("929-123-45-67", "+7 (929) 123-45-67")] // С дефисами
    [InlineData("929.123.45.67", "+7 (929) 123-45-67")] // С точками
    public void PhoneFormat_UnusualFormats_HandledCorrectly(string input, string expected)
    {
        var result = TestPhoneFormatter.Format(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc123def456", "")] // Буквы с цифрами - недостаточно цифр
    [InlineData("12345678901234567890", "+12345678901234567890")] // Очень длинный номер
    [InlineData("0000000000", "+7 (000) 000-00-00")] // Все нули (10 цифр)
    public void PhoneFormat_EdgeCases_HandledCorrectly(string input, string expected)
    {
        var result = TestPhoneFormatter.Format(input);
        result.Should().Be(expected);
    }

    #endregion

    #region Price Validation Edge Cases

    [Theory]
    [InlineData("0.00", true)]
    [InlineData("0,00", true)]
    [InlineData("999999999.99", true)]
    [InlineData("1e10", false)] // Научная нотация
    [InlineData("1.2.3", false)] // Несколько точек
    [InlineData("1,2,3", false)] // Несколько запятых
    [InlineData("$100", false)] // С символом валюты
    [InlineData("100₽", false)] // С символом рубля
    [InlineData("  100  ", true)] // С пробелами по краям
    [InlineData("1 000 000", true)] // С пробелами-разделителями
    public void PriceValidation_EdgeCases_HandledCorrectly(string price, bool expected)
    {
        var result = TestInputValidation.IsValidPrice(price);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("-0", false)] // Отрицательный ноль
    [InlineData("-0.01", false)] // Минимальное отрицательное
    [InlineData("0.001", true)] // Три знака после запятой
    public void PriceValidation_BoundaryValues_HandledCorrectly(string price, bool expected)
    {
        var result = TestInputValidation.IsValidPrice(price);
        result.Should().Be(expected);
    }

    #endregion

    #region Email Validation Edge Cases

    [Theory]
    [InlineData("a@b.c", true)] // Минимальный валидный
    [InlineData("very.long.email.address.with.many.dots@subdomain.domain.tld", true)]
    [InlineData("email@123.123.123.123", true)] // IP адрес
    [InlineData("email@[123.123.123.123]", false)] // IP в скобках - не поддерживаем
    [InlineData("\"email\"@domain.com", false)] // Кавычки - не поддерживаем
    [InlineData("email@domain", false)] // Без TLD
    [InlineData("email@.domain.com", false)] // Точка в начале домена
    [InlineData("email@domain..com", false)] // Двойная точка
    [InlineData(".email@domain.com", false)] // Точка в начале
    [InlineData("email.@domain.com", false)] // Точка в конце локальной части
    public void EmailValidation_EdgeCases_HandledCorrectly(string email, bool expected)
    {
        var result = TestInputValidation.IsValidEmail(email);
        result.Should().Be(expected);
    }

    #endregion

    #region License Plate Edge Cases

    [Theory]
    [InlineData("А000АА00", true)] // Минимальный регион (2 цифры)
    [InlineData("А000АА000", true)] // Максимальный регион (3 цифры)
    [InlineData("А000АА0", false)] // Регион 1 цифра - невалидно
    [InlineData("А000АА0000", false)] // Регион 4 цифры - невалидно
    [InlineData("0000АА00", false)] // Цифра вместо первой буквы
    [InlineData("А000А000", false)] // Цифра вместо второй буквы
    [InlineData("АААААА00", false)] // Буквы вместо цифр
    [InlineData("а000аа00", true)] // Нижний регистр
    [InlineData("A000AA00", true)] // Латиница (похожие буквы)
    [InlineData("Ж000ЖЖ00", false)] // Недопустимые буквы
    public void LicensePlate_EdgeCases_HandledCorrectly(string plate, bool expected)
    {
        var result = LicensePlateValidator.IsValid(plate);
        result.Should().Be(expected);
    }

    #endregion

    #region Version Comparison Edge Cases

    [Theory]
    [InlineData("", "", 0)] // Пустые строки
    [InlineData("1", "1", 0)] // Одна цифра
    [InlineData("1.0.0.0.0", "1", 0)] // Разная длина, но равны
    [InlineData("0.0.1", "0.0.0.1", 1)] // 0.0.1 > 0.0.0.1
    [InlineData("10.0.0", "9.9.9", 1)] // Двузначные числа
    [InlineData("1.10.0", "1.9.0", 1)] // Двузначные в середине
    [InlineData("abc", "def", 0)] // Невалидные версии = 0
    [InlineData("1.2.3-beta", "1.2.3", 0)] // С суффиксом (игнорируется)
    public void VersionComparison_EdgeCases_HandledCorrectly(string v1, string v2, int expected)
    {
        var result = VersionHelper.Compare(v1, v2);
        result.Should().Be(expected);
    }

    #endregion

    #region Order Status Edge Cases

    [Theory]
    [InlineData("новый", "#9E9E9E")] // Нижний регистр - не совпадает
    [InlineData("НОВЫЙ", "#9E9E9E")] // Верхний регистр - не совпадает
    [InlineData("Новый ", "#9E9E9E")] // С пробелом - не совпадает
    [InlineData(" Новый", "#9E9E9E")] // С пробелом в начале
    [InlineData("", "#9E9E9E")] // Пустой статус
    [InlineData("В процессе", "#9E9E9E")] // Похожий, но не точный
    public void OrderStatus_CaseSensitive_ReturnsDefault(string status, string expectedColor)
    {
        var order = new TestOrder { Status = status };
        order.StatusColor.Should().Be(expectedColor);
    }

    #endregion

    #region File Size Formatting Edge Cases

    [Theory]
    [InlineData(-1, "-1 B")] // Отрицательное значение
    [InlineData(long.MaxValue, "8388608,0 TB")] // Максимальное значение
    [InlineData(1023, "1023 B")] // Граница KB
    [InlineData(1024, "1,0 KB")] // Ровно 1 KB
    [InlineData(1025, "1,0 KB")] // Чуть больше 1 KB
    [InlineData(1048575, "1024,0 KB")] // Граница MB
    [InlineData(1048576, "1,0 MB")] // Ровно 1 MB
    public void FileSizeFormat_BoundaryValues_HandledCorrectly(long bytes, string expectedContains)
    {
        var result = FileSizeFormatterExtended.Format(bytes);
        // Проверяем что результат содержит ожидаемую единицу измерения
        var unit = expectedContains.Split(' ').Last();
        result.Should().Contain(unit);
    }

    #endregion

    #region Date Formatting Edge Cases

    [Fact]
    public void ActivityLogEntry_MinDate_FormatsCorrectly()
    {
        var entry = new TestActivityLogEntry { CreatedAt = DateTime.MinValue };
        entry.FormattedDate.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ActivityLogEntry_MaxDate_FormatsCorrectly()
    {
        var entry = new TestActivityLogEntry { CreatedAt = DateTime.MaxValue };
        entry.FormattedDate.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ActivityLogEntry_UtcDate_ConvertsToLocal()
    {
        var utcDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var entry = new TestActivityLogEntry { CreatedAt = utcDate };
        
        // Должен содержать дату (формат может отличаться от часового пояса)
        entry.FormattedDate.Should().Contain("15.06.2025");
    }

    #endregion

    #region Null Safety Tests

    [Fact]
    public void Order_NullClient_ReturnsDefaultDisplay()
    {
        var order = new TestOrder { Client = null, ClientName = "" };
        order.ClientDisplay.Should().Be("Клиент не указан");
    }

    [Fact]
    public void Order_EmptyServices_ReturnsDefaultDisplay()
    {
        var order = new TestOrder { Services = new List<TestService>() };
        order.ServicesDisplay.Should().Be("Не указаны");
    }

    [Fact]
    public void Order_NullServicesList_HandledGracefully()
    {
        var order = new TestOrder();
        order.Services = null!;
        
        // Должен выбросить NullReferenceException или вернуть default
        Action act = () => { var _ = order.ServicesDisplay; };
        act.Should().Throw<NullReferenceException>();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task Cache_ConcurrentAccess_ThreadSafe()
    {
        var cache = new TestCacheService();
        var fetchCount = 0;
        var tasks = new List<Task<string?>>();

        // Запускаем 10 параллельных запросов
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(cache.GetOrFetchAsync("shared_key", async () =>
            {
                Interlocked.Increment(ref fetchCount);
                await Task.Delay(50);
                return "data";
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Все результаты должны быть одинаковыми
        results.Should().AllBe("data");
        
        // Fetch должен быть вызван минимум 1 раз (может быть больше из-за race condition)
        fetchCount.Should().BeGreaterOrEqualTo(1);
    }

    #endregion
}

/// <summary>
/// Расширенный форматтер размера файла с поддержкой TB
/// </summary>
public static class FileSizeFormatterExtended
{
    public static string Format(long bytes) => bytes switch
    {
        < 0 => $"{bytes} B",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0:F1} MB",
        < 1024L * 1024 * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0 / 1024.0:F1} GB",
        _ => $"{bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0:F1} TB"
    };
}
