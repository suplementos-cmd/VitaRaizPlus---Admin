using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Zones;
using VitaRaiz.Application.Queries.Zones;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ZonesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ZonesController> _logger;

    public ZonesController(IMediator mediator, ILogger<ZonesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todas las zonas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetZones()
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("[ZonesController] GetZones called by {Username}", username);

            var query = new GetZonesQuery();
            var zones = await _mediator.Send(query);
            _logger.LogInformation("[ZonesController] Returning {Count} zones", zones?.Count ?? 0);
            return Ok(zones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZonesController] Error getting zones");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Obtener una zona por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var query = new GetZoneByIdQuery { ZoneId = id };
        var zone = await _mediator.Send(query);

        if (zone == null)
            return NotFound(new { message = "Zona no encontrada" });

        return Ok(zone);
    }

    /// <summary>
    /// Crear una nueva zona
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "AdminFull,Admin,Supervisor")]
    public async Task<IActionResult> CreateZone([FromBody] CreateZoneCommand command)
    {
        var zoneId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetZoneById), new { id = zoneId }, new { zoneId });
    }

    /// <summary>
    /// Actualizar una zona existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "AdminFull,Admin,Supervisor")]
    public async Task<IActionResult> UpdateZone(int id, [FromBody] UpdateZoneCommand command)
    {
        if (id != command.ZoneId)
            return BadRequest(new { message = "El ID de la zona no coincide" });

        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Zona no encontrada" });

        return NoContent();
    }

    /// <summary>
    /// Eliminar una zona
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var command = new DeleteZoneCommand { ZoneId = id };
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Zona no encontrada" });

        return NoContent();
    }
}
