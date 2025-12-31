namespace HQStudio.API.DTOs.Common;

/// <summary>
/// Generic результат операции с возможной ошибкой
/// </summary>
public record Result<T>(bool Success, T? Data, string? Error)
{
    public static Result<T> Ok(T data) => new(true, data, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}

/// <summary>
/// Результат операции без данных
/// </summary>
public record Result(bool Success, string? Error)
{
    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}
