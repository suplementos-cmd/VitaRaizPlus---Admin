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
    private readonly ILogger<SalesController> _logger;

    public SalesController(IMediator mediator, ILogger<SalesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = User.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("[SalesController] GetSales called by {Username}, Auth: {Auth}, Params: customerId={CustomerId}, sellerId={SellerId}, status={Status}", 
                username, authHeader.Substring(0, Math.Min(20, authHeader.Length)), customerId, sellerId, status);

            var query = new GetSalesQuery
            {
                CustomerId = customerId,
                SellerId = sellerId,
                StartDate = startDate,
                EndDate = endDate,
                Status = status
            };

            var sales = await _mediator.Send(query);
            
            _logger.LogInformation("[SalesController] Returning {Count} sales", sales?.Count ?? 0);
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error in GetSales");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// DEBUG: Obtener conteo de ventas directamente de la tabla
    /// </summary>
    [HttpGet("debug/count")]
    public async Task<IActionResult> GetSalesCount()
    {
        try
        {
            var query = new GetSalesQuery();
            var sales = await _mediator.Send(query);
            return Ok(new { totalSales = sales?.Count ?? 0, sales = sales });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Obtener ventas activas (con saldo pendiente)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSales([FromQuery] int? collectorId = null)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("[SalesController] GetActiveSales called by {Username}, collectorId={CollectorId}", username, collectorId);
            
            var query = new GetActiveSalesQuery { CollectorId = collectorId };
            var sales = await _mediator.Send(query);
            
            _logger.LogInformation("[SalesController] GetActiveSales returning {Count} sales", sales?.Count ?? 0);
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error in GetActiveSales");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
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
    /// Obtener venta completa con items, pagos, cliente y métricas de riesgo.
    /// Endpoint pivote para la pantalla "Gestión de Cartera".
    /// </summary>
    [HttpGet("{id}/full")]
    public async Task<IActionResult> GetSaleFull(int id)
    {
        try
        {
            var query = new GetSaleFullQuery { SaleId = id };
            var sale = await _mediator.Send(query);

            if (sale == null)
                return NotFound(new { message = "Venta no encontrada" });

            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error in GetSaleFull for saleId={SaleId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
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
    [Authorize(Roles = "Vendedor,Supervisor,AdminFull,Admin")]
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
    [Authorize(Roles = "Supervisor,AdminFull,Admin")]
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
