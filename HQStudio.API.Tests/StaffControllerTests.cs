using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

/// <summary>
/// Тесты для управления персоналом (создание/удаление пользователей)
/// </summary>
public class StaffControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateUser_WithAdminAuth_CreatesNewUser()
    {
        // Arrange
        await AuthenticateAsync();
        var newUser = new
        {
            login = "newstaff",
            password = "password123",
            name = "Новый Сотрудник",
            role = "Manager"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // API возвращает Ok, не Created
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Login.Should().Be("newstaff");
        user.Name.Should().Be("Новый Сотрудник");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateLogin_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync();
        var duplicateUser = new
        {
            login = "admin", // уже существует
            password = "password123",
            name = "Duplicate User",
            role = "Manager"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", duplicateUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var newUser = new
        {
            login = "unauthorized",
            password = "password123",
            name = "Unauthorized User",
            role = "Manager"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_WithInvalidRole_ReturnsOkWithDefaultRole()
    {
        // Arrange
        await AuthenticateAsync();
        var invalidUser = new
        {
            login = "invalidrole",
            password = "password123",
            name = "Invalid Role User",
            role = "SuperAdmin" // несуществующая роль - будет Manager по умолчанию
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", invalidUser);

        // Assert
        // API создаёт пользователя с дефолтной ролью Manager
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user!.Role.Should().Be("Manager");
    }

    [Fact]
    public async Task UpdateUser_ChangeRole_UpdatesSuccessfully()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Создаём пользователя
        var createResponse = await Client.PostAsJsonAsync("/api/users", new
        {
            login = "rolechange",
            password = "password123",
            name = "Role Change User",
            role = "Manager"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>();

        // Act - меняем роль
        var updateResponse = await Client.PutAsJsonAsync($"/api/users/{created!.Id}", new
        {
            name = "Role Change User",
            role = "Editor",
            password = (string?)null
        });

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_CreatedUser_SoftDeletesSuccessfully()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Создаём пользователя для удаления
        var createResponse = await Client.PostAsJsonAsync("/api/users", new
        {
            login = "todelete",
            password = "password123",
            name = "To Delete User",
            role = "Manager"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>();

        // Act - soft delete (деактивация)
        var deleteResponse = await Client.DeleteAsync($"/api/users/{created!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft deletion - пользователь всё ещё существует, но деактивирован
        var getResponse = await Client.GetAsync($"/api/users/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedUser = await getResponse.Content.ReadFromJsonAsync<UserResponse>();
        deletedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetUsers_ReturnsAllRoles()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/users");
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();

        // Assert
        users.Should().NotBeNull();
        users.Should().Contain(u => u.Role == "Admin");
    }

    private class UserResponse
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
