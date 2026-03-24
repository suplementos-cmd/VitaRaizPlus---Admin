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

    public ZonesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener todas las zonas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetZones()
    {
        var query = new GetZonesQuery();
        var zones = await _mediator.Send(query);
        return Ok(zones);
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
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> CreateZone([FromBody] CreateZoneCommand command)
    {
        var zoneId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetZoneById), new { id = zoneId }, new { zoneId });
    }

    /// <summary>
    /// Actualizar una zona existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var command = new DeleteZoneCommand { ZoneId = id };
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Zona no encontrada" });

        return NoContent();
    }
}
