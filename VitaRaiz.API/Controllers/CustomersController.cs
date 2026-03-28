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
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(IMediator mediator, ILogger<CustomersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = User.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("[CustomersController] GetCustomers called by {Username}, Auth: {Auth}, Params: searchTerm={SearchTerm}, zoneId={ZoneId}", 
                username, authHeader.Substring(0, Math.Min(20, authHeader.Length)), searchTerm, zoneId);

            var query = new GetCustomersQuery
            {
                SearchTerm = searchTerm,
                ZoneId = zoneId,
                IsGoldCustomer = isGoldCustomer,
                IsBlacklisted = isBlacklisted
            };

            var customers = await _mediator.Send(query);
            
            _logger.LogInformation("[CustomersController] Returning {Count} customers", customers?.Count ?? 0);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomersController] Error in GetCustomers");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
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
    [Authorize] // TEMPORAL: Permite cualquier usuario autenticado crear clientes
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        try
        {
            // Extract user ID from JWT claims for CREATED_BY
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                command.CreatedBy = userId;
            }

            _logger.LogInformation("[CustomersController] CreateCustomer: {Name}, Phone: {Phone}, Zone: {Zone}, CreatedBy: {CreatedBy}", 
                command.CustomerName, command.PhoneNumber, command.ZoneId, command.CreatedBy);
            var customerId = await _mediator.Send(command);
            _logger.LogInformation("[CustomersController] Customer created with ID: {Id}", customerId);
            return CreatedAtAction(nameof(GetCustomerById), new { id = customerId }, new { customerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomersController] Error creating customer");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Actualizar un cliente existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "AdminFull,Admin,Supervisor")]
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
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var command = new DeleteCustomerCommand { CustomerId = id };
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Cliente no encontrado" });

        return NoContent();
    }
}
