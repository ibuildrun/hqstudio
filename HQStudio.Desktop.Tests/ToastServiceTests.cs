using FluentAssertions;
using Xunit;
using System.Collections.ObjectModel;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для логики ToastService (без зависимости от WPF Dispatcher)
/// </summary>
public class ToastServiceTests
{
    [Fact]
    public void ShowSuccess_AddsNotificationToQueue()
    {
        var service = new TestToastService();
        
        service.ShowSuccess("Успешно сохранено");
        
        service.Notifications.Should().HaveCount(1);
        service.Notifications[0].Message.Should().Be("Успешно сохранено");
        service.Notifications[0].Type.Should().Be(TestToastType.Success);
    }

    [Fact]
    public void ShowInfo_AddsNotificationToQueue()
    {
        var service = new TestToastService();
        
        service.ShowInfo("Информация");
        
        service.Notifications.Should().HaveCount(1);
        service.Notifications[0].Type.Should().Be(TestToastType.Info);
    }

    [Fact]
    public void ShowWarning_AddsNotificationToQueue()
    {
        var service = new TestToastService();
        
        service.ShowWarning("Предупреждение");
        
        service.Notifications.Should().HaveCount(1);
        service.Notifications[0].Type.Should().Be(TestToastType.Warning);
    }

    [Fact]
    public void ShowError_AddsNotificationToQueue()
    {
        var service = new TestToastService();
        
        service.ShowError("Ошибка");
        
        service.Notifications.Should().HaveCount(1);
        service.Notifications[0].Type.Should().Be(TestToastType.Error);
    }

    [Fact]
    public void MultipleNotifications_AddedInOrder()
    {
        var service = new TestToastService();
        
        service.ShowSuccess("Первое");
        service.ShowInfo("Второе");
        service.ShowError("Третье");
        
        service.Notifications.Should().HaveCount(3);
        service.Notifications[0].Message.Should().Be("Первое");
        service.Notifications[1].Message.Should().Be("Второе");
        service.Notifications[2].Message.Should().Be("Третье");
    }

    [Fact]
    public void MaxNotifications_RemovesOldest()
    {
        var service = new TestToastService(maxNotifications: 3);
        
        service.ShowInfo("1");
        service.ShowInfo("2");
        service.ShowInfo("3");
        service.ShowInfo("4"); // Должен удалить первый
        
        service.Notifications.Should().HaveCount(3);
        service.Notifications[0].Message.Should().Be("2");
        service.Notifications[1].Message.Should().Be("3");
        service.Notifications[2].Message.Should().Be("4");
    }

    [Fact]
    public void Dismiss_RemovesNotification()
    {
        var service = new TestToastService();
        
        service.ShowSuccess("Тест");
        var notification = service.Notifications[0];
        
        service.Dismiss(notification);
        
        service.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void ClearAll_RemovesAllNotifications()
    {
        var service = new TestToastService();
        
        service.ShowSuccess("1");
        service.ShowInfo("2");
        service.ShowError("3");
        
        service.ClearAll();
        
        service.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void Notification_HasUniqueId()
    {
        var service = new TestToastService();
        
        service.ShowSuccess("1");
        service.ShowSuccess("2");
        
        service.Notifications[0].Id.Should().NotBe(service.Notifications[1].Id);
    }

    [Fact]
    public void Notification_HasCreatedAtTimestamp()
    {
        var service = new TestToastService();
        var before = DateTime.Now;
        
        service.ShowSuccess("Тест");
        
        var after = DateTime.Now;
        service.Notifications[0].CreatedAt.Should().BeOnOrAfter(before);
        service.Notifications[0].CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Notification_IsVisibleByDefault()
    {
        var service = new TestToastService();
        
        service.ShowSuccess("Тест");
        
        service.Notifications[0].IsVisible.Should().BeTrue();
    }
}

/// <summary>
/// Тип toast-уведомления для тестов
/// </summary>
public enum TestToastType
{
    Success,
    Info,
    Warning,
    Error
}

/// <summary>
/// Модель toast-уведомления для тестов
/// </summary>
public class TestToastNotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public TestToastType Type { get; set; }
    public DateTime CreatedAt { get; } = DateTime.Now;
    public bool IsVisible { get; set; } = true;
}

/// <summary>
/// Тестовая реализация ToastService без зависимости от WPF
/// </summary>
public class TestToastService
{
    private readonly int _maxNotifications;

    public ObservableCollection<TestToastNotification> Notifications { get; } = new();

    public TestToastService(int maxNotifications = 5)
    {
        _maxNotifications = maxNotifications;
    }

    public void ShowSuccess(string message) => Show(message, TestToastType.Success);
    public void ShowInfo(string message) => Show(message, TestToastType.Info);
    public void ShowWarning(string message) => Show(message, TestToastType.Warning);
    public void ShowError(string message) => Show(message, TestToastType.Error);

    public void Show(string message, TestToastType type)
    {
        var notification = new TestToastNotification
        {
            Message = message,
            Type = type
        };

        while (Notifications.Count >= _maxNotifications)
        {
            Notifications.RemoveAt(0);
        }

        Notifications.Add(notification);
    }

    public void Dismiss(TestToastNotification notification)
    {
        notification.IsVisible = false;
        Notifications.Remove(notification);
    }

    public void ClearAll()
    {
        Notifications.Clear();
    }
}
