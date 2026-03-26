using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Payments;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Queries.Payments;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener todos los pagos con filtros opcionales
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int? saleId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int? collectorId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = User.Identity?.Name ?? "Anonymous";
            Console.WriteLine($"[PaymentsController] GetPayments called by {username}, Params: collectorId={collectorId}, startDate={startDate}, endDate={endDate}");

            var query = new GetPaymentsQuery
            {
                SaleId = saleId,
                CustomerId = customerId,
                CollectorId = collectorId,
                StartDate = startDate,
                EndDate = endDate,
                Status = status
            };

            var payments = await _mediator.Send(query);
            Console.WriteLine($"[PaymentsController] Returning {payments?.Count ?? 0} payments");
            return Ok(payments);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentsController] ERROR: {ex.Message}");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Obtener un pago por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentById(int id)
    {
        var query = new GetPaymentByIdQuery { PaymentId = id };
        var payment = await _mediator.Send(query);

        if (payment == null)
            return NotFound(new { message = "Pago no encontrado" });

        return Ok(payment);
    }

    /// <summary>
    /// Registrar un nuevo pago para una venta
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Cobrador,Supervisor,AdminFull,Admin")]
    public async Task<ActionResult<PaymentDto>> RegisterPayment([FromBody] RegisterPaymentCommand command)
    {
        try
        {
            var paymentId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetPaymentById), new { id = paymentId }, new { paymentId, message = "Pago registrado exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aprobar un pago
    /// </summary>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Supervisor,AdminFull,Admin")]
    public async Task<IActionResult> ApprovePayment(int id, [FromBody] ApprovePaymentCommand command)
    {
        if (id != command.PaymentId)
            return BadRequest(new { message = "El ID del pago no coincide" });

        try
        {
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { message = "Pago no encontrado" });

            return Ok(new { message = "Pago aprobado exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rechazar un pago
    /// </summary>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Supervisor,AdminFull,Admin")]
    public async Task<IActionResult> RejectPayment(int id, [FromBody] RejectPaymentCommand command)
    {
        if (id != command.PaymentId)
            return BadRequest(new { message = "El ID del pago no coincide" });

        try
        {
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { message = "Pago no encontrado" });

            return Ok(new { message = "Pago rechazado" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
