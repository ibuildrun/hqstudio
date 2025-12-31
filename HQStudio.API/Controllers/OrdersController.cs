using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HQStudio.API.Attributes;
using HQStudio.API.DTOs.Orders;
using HQStudio.API.Extensions;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    [HttpGet]
    [DesktopOrAuthorize]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] OrderStatus? status = null, 
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false)
    {
        var filter = new OrderFilterRequest(status, page, pageSize, includeDeleted);
        var result = await _orderService.GetAllAsync(filter);
        
        return Ok(new
        {
            items = result.Items,
            total = result.Total,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }

    [HttpGet("{id}")]
    [DesktopOrAuthorize]
    public async Task<ActionResult<OrderDto>> Get(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [DesktopOrAuthorize]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        var order = await _orderService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPut("{id}/status")]
    [DesktopOrAuthorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var success = await _orderService.UpdateStatusAsync(id, status);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Soft delete - помечает заказ как удалённый, но не удаляет из базы
    /// </summary>
    [HttpDelete("{id}")]
    [DesktopOrAuthorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.GetCurrentUserId();
        var success = await _orderService.SoftDeleteAsync(id, userId > 0 ? userId : null);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Восстановление удалённого заказа
    /// </summary>
    [HttpPost("{id}/restore")]
    [DesktopOrAuthorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var success = await _orderService.RestoreAsync(id);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Полное удаление заказа (только для разработчиков)
    /// </summary>
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        var success = await _orderService.PermanentDeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Удаление заказов без клиентов (очистка базы)
    /// </summary>
    [HttpDelete("cleanup/without-clients")]
    [DesktopOnly]
    public async Task<ActionResult<CleanupResult>> CleanupOrdersWithoutClients()
    {
        var result = await _orderService.CleanupOrdersWithoutClientsAsync();
        return Ok(result);
    }
}
