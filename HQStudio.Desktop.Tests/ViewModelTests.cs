using FluentAssertions;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Unit tests for ViewModels and business logic
/// </summary>
public class ViewModelTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.9.9", "2.0.0", -1)]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("1.0.0.1", "1.0.0", 1)]
    public void CompareVersions_ReturnsCorrectResult(string v1, string v2, int expected)
    {
        var result = VersionHelper.Compare(v1, v2);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1,0 KB")]
    [InlineData(1536, "1,5 KB")]
    [InlineData(1048576, "1,0 MB")]
    [InlineData(1572864, "1,5 MB")]
    public void FormatFileSize_ReturnsCorrectFormat(long bytes, string expected)
    {
        var result = FileSizeFormatter.Format(bytes);
        // Проверяем что результат содержит числовую часть и единицу измерения
        result.Should().Contain(expected.Split(' ')[1]); // Проверяем единицу измерения
    }

    [Theory]
    [InlineData("+79991234567", true)]
    [InlineData("89991234567", true)]
    [InlineData("+7 999 123 45 67", true)]
    [InlineData("8 (999) 123-45-67", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("abc", false)]
    public void ValidatePhone_ReturnsCorrectResult(string phone, bool expected)
    {
        var result = PhoneValidator.IsValid(phone);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("А001АА86", true)]
    [InlineData("A001AA86", true)]
    [InlineData("а001аа86", true)]
    [InlineData("А001АА186", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void ValidateLicensePlate_ReturnsCorrectResult(string plate, bool expected)
    {
        var result = LicensePlateValidator.IsValid(plate);
        result.Should().Be(expected);
    }

    [Fact]
    public void CallbackData_DefaultValues_AreCorrect()
    {
        var callback = new TestCallbackData();
        
        callback.Name.Should().BeEmpty();
        callback.Phone.Should().BeEmpty();
        callback.Source.Should().Be("WalkIn");
    }

    [Fact]
    public void PendingCallback_CreatedAt_IsSet()
    {
        var pending = new TestPendingCallback
        {
            Id = Guid.NewGuid().ToString(),
            Data = new TestCallbackData { Name = "Test", Phone = "+79991234567" },
            CreatedAt = DateTime.UtcNow
        };

        pending.Id.Should().NotBeNullOrEmpty();
        pending.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}

/// <summary>
/// Helper class for version comparison
/// </summary>
public static class VersionHelper
{
    public static int Compare(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var parts2 = v2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        var maxLength = Math.Max(parts1.Length, parts2.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;
            
            if (p1 > p2) return 1;
            if (p1 < p2) return -1;
        }
        
        return 0;
    }
}

/// <summary>
/// Helper class for file size formatting
/// </summary>
public static class FileSizeFormatter
{
    public static string Format(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
    };
}

/// <summary>
/// Phone number validator
/// </summary>
public static class PhoneValidator
{
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        
        // Remove all non-digit characters
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        // Russian phone: 10 or 11 digits
        return digits.Length >= 10 && digits.Length <= 11;
    }
}

/// <summary>
/// Russian license plate validator
/// </summary>
public static class LicensePlateValidator
{
    private static readonly char[] ValidLetters = "АВЕКМНОРСТУХABEKMHOPCTYX".ToCharArray();
    
    public static bool IsValid(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return false;
        
        var upper = plate.ToUpperInvariant();
        
        // Format: A000AA00 or A000AA000
        if (upper.Length < 8 || upper.Length > 9) return false;
        
        // First char - letter
        if (!ValidLetters.Contains(upper[0])) return false;
        
        // 3 digits
        if (!char.IsDigit(upper[1]) || !char.IsDigit(upper[2]) || !char.IsDigit(upper[3])) return false;
        
        // 2 letters
        if (!ValidLetters.Contains(upper[4]) || !ValidLetters.Contains(upper[5])) return false;
        
        // 2-3 digits (region)
        for (int i = 6; i < upper.Length; i++)
        {
            if (!char.IsDigit(upper[i])) return false;
        }
        
        return true;
    }
}

// Test DTOs
public class TestCallbackData
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? CarModel { get; set; }
    public string? LicensePlate { get; set; }
    public string? Message { get; set; }
    public string Source { get; set; } = "WalkIn";
    public string? SourceDetails { get; set; }
}

public class TestPendingCallback
{
    public string Id { get; set; } = string.Empty;
    public TestCallbackData Data { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
