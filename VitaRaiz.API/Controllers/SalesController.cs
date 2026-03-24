using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
