using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Customers;
using VitaRaiz.Application.Queries.Customers;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener todos los clientes con filtros opcionales
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? zoneId = null,
        [FromQuery] bool? isGoldCustomer = null,
        [FromQuery] bool? isBlacklisted = null)
    {
        var query = new GetCustomersQuery
        {
            SearchTerm = searchTerm,
            ZoneId = zoneId,
            IsGoldCustomer = isGoldCustomer,
            IsBlacklisted = isBlacklisted
        };

        var customers = await _mediator.Send(query);
        return Ok(customers);
    }

    /// <summary>
    /// Obtener un cliente por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var query = new GetCustomerByIdQuery { CustomerId = id };
        var customer = await _mediator.Send(query);

        if (customer == null)
            return NotFound(new { message = "Cliente no encontrado" });

        return Ok(customer);
    }

    /// <summary>
    /// Crear un nuevo cliente
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor,Vendedor")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var customerId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCustomerById), new { id = customerId }, new { customerId });
    }

    /// <summary>
    /// Actualizar un cliente existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerCommand command)
    {
        if (id != command.CustomerId)
            return BadRequest(new { message = "El ID del cliente no coincide" });

        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Cliente no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Eliminar un cliente
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var command = new DeleteCustomerCommand { CustomerId = id };
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Cliente no encontrado" });

        return NoContent();
    }
}
