using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Payments;
using VitaRaiz.Application.DTOs;

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
    /// Registrar un nuevo pago para una venta
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Cobrador,Supervisor")]
    public async Task<ActionResult<PaymentDto>> RegisterPayment([FromBody] RegisterPaymentCommand command)
    {
        try
        {
            var paymentId = await _mediator.Send(command);
            return Ok(new { paymentId, message = "Pago registrado exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
