using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HQStudio.API.Attributes;
using HQStudio.API.DTOs.Clients;
using HQStudio.API.Extensions;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService) => _clientService = clientService;

    [HttpGet]
    [DesktopOrAuthorize]
    public async Task<ActionResult<List<ClientDto>>> GetAll([FromQuery] int? limit = null)
    {
        var isDesktop = HttpContext.IsDesktopClient();
        var clients = await _clientService.GetAllAsync(limit, isDesktop);
        return Ok(clients);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ClientDto>> Get(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        return client == null ? NotFound() : Ok(client);
    }

    [HttpPost]
    [DesktopOrAuthorize]
    public async Task<ActionResult<ClientDto>> Create(CreateClientRequest request)
    {
        var result = await _clientService.CreateAsync(request);
        
        if (!result.Success)
        {
            return Conflict(new { message = result.Error });
        }
        
        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, UpdateClientRequest request)
    {
        var success = await _clientService.UpdateAsync(id, request);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _clientService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}
