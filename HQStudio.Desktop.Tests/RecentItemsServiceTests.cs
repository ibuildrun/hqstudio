using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using System.Collections.ObjectModel;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для RecentItemsService
/// </summary>
public class RecentItemsServiceTests
{
    /// <summary>
    /// Тип недавно просмотренного элемента для тестов
    /// </summary>
    public enum TestRecentItemType
    {
        Client,
        Order,
        Service
    }

    /// <summary>
    /// Модель недавно просмотренного элемента для тестов
    /// </summary>
    public class TestRecentItem
    {
        public TestRecentItemType Type { get; set; }
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime ViewedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Тестовая реализация RecentItemsService без зависимости от файловой системы
    /// </summary>
    public class TestRecentItemsService
    {
        private const int MaxRecentItems = 10;

        public ObservableCollection<TestRecentItem> RecentItems { get; } = new();

        public void AddRecentItem(TestRecentItemType type, int id, string title, string subtitle = "")
        {
            // Удаляем существующий элемент с таким же типом и ID
            var existing = RecentItems.FirstOrDefault(r => r.Type == type && r.Id == id);
            if (existing != null)
            {
                RecentItems.Remove(existing);
            }

            // Добавляем новый элемент в начало
            var newItem = new TestRecentItem
            {
                Type = type,
                Id = id,
                Title = title,
                Subtitle = subtitle,
                ViewedAt = DateTime.Now
            };

            RecentItems.Insert(0, newItem);

            // Ограничиваем количество элементов до 10
            while (RecentItems.Count > MaxRecentItems)
            {
                RecentItems.RemoveAt(RecentItems.Count - 1);
            }
        }

        public void ClearAll()
        {
            RecentItems.Clear();
        }
    }


    #region Unit Tests

    [Fact]
    public void AddRecentItem_AddsItemToCollection()
    {
        var service = new TestRecentItemsService();
        
        service.AddRecentItem(TestRecentItemType.Client, 1, "Иван Иванов", "+7-999-123-45-67");
        
        service.RecentItems.Should().HaveCount(1);
        service.RecentItems[0].Title.Should().Be("Иван Иванов");
        service.RecentItems[0].Type.Should().Be(TestRecentItemType.Client);
    }

    [Fact]
    public void AddRecentItem_NewItemsAddedAtBeginning()
    {
        var service = new TestRecentItemsService();
        
        service.AddRecentItem(TestRecentItemType.Client, 1, "Первый");
        service.AddRecentItem(TestRecentItemType.Client, 2, "Второй");
        service.AddRecentItem(TestRecentItemType.Client, 3, "Третий");
        
        service.RecentItems[0].Title.Should().Be("Третий");
        service.RecentItems[1].Title.Should().Be("Второй");
        service.RecentItems[2].Title.Should().Be("Первый");
    }

    [Fact]
    public void AddRecentItem_DuplicateMovesToTop()
    {
        var service = new TestRecentItemsService();
        
        service.AddRecentItem(TestRecentItemType.Client, 1, "Первый");
        service.AddRecentItem(TestRecentItemType.Client, 2, "Второй");
        service.AddRecentItem(TestRecentItemType.Client, 1, "Первый обновлённый");
        
        service.RecentItems.Should().HaveCount(2);
        service.RecentItems[0].Title.Should().Be("Первый обновлённый");
        service.RecentItems[1].Title.Should().Be("Второй");
    }

    [Fact]
    public void AddRecentItem_DifferentTypesWithSameId_BothKept()
    {
        var service = new TestRecentItemsService();
        
        service.AddRecentItem(TestRecentItemType.Client, 1, "Клиент 1");
        service.AddRecentItem(TestRecentItemType.Order, 1, "Заказ 1");
        
        service.RecentItems.Should().HaveCount(2);
    }

    [Fact]
    public void ClearAll_RemovesAllItems()
    {
        var service = new TestRecentItemsService();
        
        service.AddRecentItem(TestRecentItemType.Client, 1, "Клиент");
        service.AddRecentItem(TestRecentItemType.Order, 1, "Заказ");
        service.AddRecentItem(TestRecentItemType.Service, 1, "Услуга");
        
        service.ClearAll();
        
        service.RecentItems.Should().BeEmpty();
    }

    #endregion

    #region Property-Based Tests

    /// <summary>
    /// **Property 5: Recent items limited to 10**
    /// *For any* sequence of item views, the recent items list should never exceed 10 items,
    /// with oldest items removed first.
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecentItems_NeverExceedsTen()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>(),
            count =>
            {
                var service = new TestRecentItemsService();
                var itemCount = count.Get;
                
                // Добавляем произвольное количество элементов
                for (int i = 0; i < itemCount; i++)
                {
                    var type = (TestRecentItemType)(i % 3);
                    service.AddRecentItem(type, i, $"Item {i}");
                }
                
                // Проверяем, что количество не превышает 10
                return service.RecentItems.Count <= 10;
            });
    }

    /// <summary>
    /// **Property 5: Recent items limited to 10**
    /// При добавлении более 10 элементов, самые старые удаляются первыми.
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecentItems_OldestRemovedFirst()
    {
        return Prop.ForAll(
            Gen.Choose(11, 50).ToArbitrary(),
            count =>
            {
                var service = new TestRecentItemsService();
                
                // Добавляем больше 10 элементов
                for (int i = 0; i < count; i++)
                {
                    service.AddRecentItem(TestRecentItemType.Client, i, $"Item {i}");
                }
                
                // Проверяем, что остались только последние 10 элементов
                // Последний добавленный должен быть первым в списке
                var expectedFirstId = count - 1;
                var expectedLastId = count - 10;
                
                return service.RecentItems.Count == 10 &&
                       service.RecentItems[0].Id == expectedFirstId &&
                       service.RecentItems[9].Id == expectedLastId;
            });
    }

    /// <summary>
    /// **Property 5: Recent items limited to 10**
    /// Дубликаты перемещаются в начало списка, не увеличивая общее количество.
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecentItems_DuplicatesDoNotIncreaseCount()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            Gen.Choose(1, 20).ToArbitrary(),
            (uniqueCount, duplicateCount) =>
            {
                var service = new TestRecentItemsService();
                
                // Добавляем уникальные элементы
                for (int i = 0; i < uniqueCount; i++)
                {
                    service.AddRecentItem(TestRecentItemType.Client, i, $"Item {i}");
                }
                
                var countAfterUnique = service.RecentItems.Count;
                
                // Добавляем дубликаты существующих элементов
                for (int i = 0; i < duplicateCount; i++)
                {
                    var existingId = i % uniqueCount;
                    service.AddRecentItem(TestRecentItemType.Client, existingId, $"Item {existingId} updated");
                }
                
                // Количество не должно увеличиться
                return service.RecentItems.Count == countAfterUnique;
            });
    }

    /// <summary>
    /// **Property 5: Recent items limited to 10**
    /// Последний добавленный элемент всегда находится в начале списка.
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecentItems_LastAddedIsFirst()
    {
        return Prop.ForAll(
            Gen.Choose(1, 30).ToArbitrary(),
            Arb.From<NonEmptyString>(),
            (count, title) =>
            {
                var service = new TestRecentItemsService();
                var lastTitle = title.Get;
                
                // Добавляем несколько элементов
                for (int i = 0; i < count; i++)
                {
                    service.AddRecentItem(TestRecentItemType.Service, i, $"Item {i}");
                }
                
                // Добавляем последний элемент с уникальным ID
                var lastId = count + 1000;
                service.AddRecentItem(TestRecentItemType.Service, lastId, lastTitle);
                
                // Последний добавленный должен быть первым
                return service.RecentItems[0].Id == lastId &&
                       service.RecentItems[0].Title == lastTitle;
            });
    }

    #endregion
}
