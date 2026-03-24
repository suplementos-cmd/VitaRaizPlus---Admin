using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Sales;
using VitaRaiz.Application.Queries.Sales;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener todas las ventas con filtros opcionales
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] int? customerId = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null)
    {
        var query = new GetSalesQuery
        {
            CustomerId = customerId,
            SellerId = sellerId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status
        };

        var sales = await _mediator.Send(query);
        return Ok(sales);
    }

    /// <summary>
    /// Obtener ventas activas (con saldo pendiente)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSales([FromQuery] int? collectorId = null)
    {
        var query = new GetActiveSalesQuery { CollectorId = collectorId };
        var sales = await _mediator.Send(query);
        return Ok(sales);
    }

    /// <summary>
    /// Obtener una venta por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSaleById(int id)
    {
        var query = new GetSaleByIdQuery { SaleId = id };
        var sale = await _mediator.Send(query);

        if (sale == null)
            return NotFound(new { message = "Venta no encontrada" });

        return Ok(sale);
    }

    /// <summary>
    /// Obtener el balance pendiente de una venta
    /// </summary>
    [HttpGet("{saleId}/balance")]
    public async Task<ActionResult<decimal>> GetSaleBalance(int saleId)
    {
        try
        {
            var query = new GetSaleBalanceQuery(saleId);
            var balance = await _mediator.Send(query);
            return Ok(new { saleId, balance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crear una nueva venta con detalles
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Vendedor,Supervisor,Admin")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleCommand command)
    {
        try
        {
            var saleId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetSaleById), new { id = saleId }, new { saleId, message = "Venta creada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancelar una venta
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Supervisor,Admin")]
    public async Task<IActionResult> CancelSale(int id, [FromBody] CancelSaleCommand command)
    {
        if (id != command.SaleId)
            return BadRequest(new { message = "El ID de la venta no coincide" });

        try
        {
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { message = "Venta no encontrada" });

            return Ok(new { message = "Venta cancelada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
