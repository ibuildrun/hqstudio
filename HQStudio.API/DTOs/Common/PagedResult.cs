namespace HQStudio.API.DTOs.Common;

/// <summary>
/// Generic результат пагинации
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}
