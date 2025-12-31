using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HQStudio.API.Attributes;
using HQStudio.API.DTOs.Services;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IServiceService serviceService, ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetAll([FromQuery] bool activeOnly = false)
    {
        _logger.LogInformation("[Services] GET all, activeOnly={ActiveOnly}", activeOnly);
        var services = await _serviceService.GetAllAsync(activeOnly);
        _logger.LogInformation("[Services] Returning {Count} services", services.Count);
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceDto>> Get(int id)
    {
        _logger.LogInformation("[Services] GET {Id}", id);
        var service = await _serviceService.GetByIdAsync(id);
        
        if (service == null)
        {
            _logger.LogWarning("[Services] Service {Id} not found", id);
            return NotFound();
        }
        
        _logger.LogInformation("[Services] Found service: Id={Id}, Title={Title}", service.Id, service.Title);
        return Ok(service);
    }

    [HttpPost]
    [DesktopOrAuthorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<ServiceDto>> Create(Service service)
    {
        _logger.LogInformation("[Services] POST Create: Title={Title}, Icon={Icon}", service.Title, service.Icon);
        
        var created = await _serviceService.CreateAsync(service);
        _logger.LogInformation("[Services] Created service Id={Id}", created.Id);
        
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [DesktopOrAuthorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Update(int id, Service service)
    {
        _logger.LogInformation("[Services] PUT Update {Id}: Title={Title}, Icon={Icon}", id, service.Title, service.Icon);
        
        if (id != service.Id)
        {
            _logger.LogWarning("[Services] ID mismatch: URL={UrlId}, Body={BodyId}", id, service.Id);
            return BadRequest(new { message = $"ID mismatch: URL={id}, Body={service.Id}" });
        }
        
        var success = await _serviceService.UpdateAsync(id, service);
        
        if (!success)
        {
            _logger.LogWarning("[Services] Service {Id} not found for update", id);
            return NotFound();
        }
        
        _logger.LogInformation("[Services] Successfully updated service {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [DesktopOrAuthorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("[Services] DELETE {Id}", id);
        
        var success = await _serviceService.DeleteAsync(id);
        
        if (!success)
        {
            _logger.LogWarning("[Services] Service {Id} not found for delete", id);
            return NotFound();
        }
        
        _logger.LogInformation("[Services] Successfully deleted service {Id}", id);
        return NoContent();
    }
}
